import { useEffect, useRef, useState, type FormEvent } from 'react'
import * as channelsApi from '../../api/channels'
import { getErrorMessage } from '../../api/errors'
import FormField from '../FormField'
import { Alert } from '../ui/Alert'
import { Button, IconButton } from '../ui/Button'
import { IconX } from '../ui/icons'

// Column limit from GameHubDbContext (User.Username).
const USERNAME_MAX_LENGTH = 50

// Matches the .modal-closing animation in index.css.
const EXIT_DURATION_MS = 180

interface AddMemberModalProps {
  channelId: string
  channelName: string
  onClose: () => void
}

// Adds people to a private channel by username. Stays open after each
// success so several members can be added in a row; closing returns to the
// channel. Only offered for private channels, but the backend is what
// enforces who may add members.
function AddMemberModal({ channelId, channelName, onClose }: AddMemberModalProps) {
  const dialogRef = useRef<HTMLDialogElement>(null)
  const [username, setUsername] = useState('')
  const [usernameError, setUsernameError] = useState<string | undefined>()
  const [formError, setFormError] = useState<string | null>(null)
  const [addedUsername, setAddedUsername] = useState<string | null>(null)
  const [isSubmitting, setIsSubmitting] = useState(false)
  const [isClosing, setIsClosing] = useState(false)
  const closeTimerRef = useRef<number | undefined>(undefined)

  // Same dialog behavior as CreateChannelModal: native <dialog> for focus,
  // Esc and backdrop; the open check keeps StrictMode from throwing.
  useEffect(() => {
    const dialog = dialogRef.current

    if (dialog && !dialog.open) {
      dialog.showModal()
    }
  }, [])

  useEffect(() => () => window.clearTimeout(closeTimerRef.current), [])

  function closeAnimated() {
    const reduceMotion = window.matchMedia('(prefers-reduced-motion: reduce)').matches

    setIsClosing(true)
    closeTimerRef.current = window.setTimeout(
      () => {
        dialogRef.current?.close()
        onClose()
      },
      reduceMotion ? 0 : EXIT_DURATION_MS,
    )
  }

  function handleCancel() {
    if (!isClosing) {
      closeAnimated()
    }
  }

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()

    if (isClosing) {
      return
    }

    const trimmedUsername = username.trim()

    if (!trimmedUsername) {
      setUsernameError('Informe o nome de usuário.')
      return
    }

    setUsernameError(undefined)
    setFormError(null)
    setAddedUsername(null)
    setIsSubmitting(true)

    try {
      const member = await channelsApi.addMember(channelId, {
        username: trimmedUsername,
      })

      setAddedUsername(member.username)
      setUsername('')
    } catch (error) {
      // 404: no user with that name (the channel itself is always accessible
      // here); 409: already a member.
      setFormError(
        getErrorMessage(error, {
          404: 'Nenhum usuário encontrado com esse nome.',
          409: 'Esse usuário já é membro do canal.',
        }),
      )
    } finally {
      setIsSubmitting(false)
    }
  }

  return (
    <dialog
      ref={dialogRef}
      className={isClosing ? 'modal modal-closing' : 'modal'}
      aria-labelledby="add-member-title"
      aria-describedby="add-member-description"
      onClose={onClose}
      onCancel={(event) => {
        event.preventDefault()

        if (!isSubmitting) {
          handleCancel()
        }
      }}
    >
      <form onSubmit={handleSubmit} noValidate aria-busy={isSubmitting}>
        <div className="modal-heading">
          <h2 id="add-member-title">Adicionar membro</h2>
          <p id="add-member-description" className="modal-subtitle">
            Membros de <strong>#{channelName}</strong> veem o canal e as mensagens.
          </p>
        </div>

        {formError && (
          <Alert variant="error" role="alert">
            {formError}
          </Alert>
        )}

        {addedUsername && (
          <Alert variant="success" role="status">
            <strong>{addedUsername}</strong> agora é membro do canal.
          </Alert>
        )}

        <FormField
          id="member-username"
          label="Nome de usuário"
          type="text"
          value={username}
          onChange={setUsername}
          autoComplete="off"
          error={usernameError}
          maxLength={USERNAME_MAX_LENGTH}
        />

        <div className="modal-actions">
          <Button variant="secondary" onClick={handleCancel} disabled={isSubmitting}>
            Fechar
          </Button>
          <Button type="submit" isLoading={isSubmitting}>
            {isSubmitting ? 'Adicionando…' : 'Adicionar'}
          </Button>
        </div>
      </form>

      <IconButton
        label="Fechar"
        icon={<IconX />}
        size="sm"
        className="modal-close"
        onClick={handleCancel}
        disabled={isSubmitting}
      />
    </dialog>
  )
}

export default AddMemberModal
