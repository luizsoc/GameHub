import { useEffect, useRef, useState, type FormEvent } from 'react'
import * as channelsApi from '../../api/channels'
import { getErrorMessage } from '../../api/errors'
import type { ChannelResponse } from '../../types/channel'
import FormField from '../FormField'
import { Alert } from '../ui/Alert'
import { Button, IconButton } from '../ui/Button'
import { IconX } from '../ui/icons'

// Column limits from GameHubDbContext (Channel.Name / Channel.Description).
const NAME_MAX_LENGTH = 100
const DESCRIPTION_MAX_LENGTH = 500

// Matches the .modal-closing animation in index.css.
const EXIT_DURATION_MS = 180

interface CreateChannelModalProps {
  onClose: () => void
  onCreated: (channel: ChannelResponse) => void
}

function CreateChannelModal({ onClose, onCreated }: CreateChannelModalProps) {
  const dialogRef = useRef<HTMLDialogElement>(null)
  const [name, setName] = useState('')
  const [description, setDescription] = useState('')
  const [nameError, setNameError] = useState<string | undefined>()
  const [formError, setFormError] = useState<string | null>(null)
  const [isSubmitting, setIsSubmitting] = useState(false)
  const [isClosing, setIsClosing] = useState(false)
  const closeTimerRef = useRef<number | undefined>(undefined)

  // Native <dialog> gives focus handling, Esc to close and a backdrop.
  // The open check keeps StrictMode's double effect from throwing.
  useEffect(() => {
    const dialog = dialogRef.current

    if (dialog && !dialog.open) {
      dialog.showModal()
    }
  }, [])

  useEffect(() => () => window.clearTimeout(closeTimerRef.current), [])

  // Plays the exit animation, then closes for real. Native close() returns
  // focus to the button that opened the dialog; `after` then unmounts it right
  // away instead of waiting for the async "close" event (which Chromium only
  // delivers on the next rendered frame). A timer, not animationend, so the
  // dialog still closes when the page is not being painted.
  function closeAnimated(after: () => void) {
    const reduceMotion = window.matchMedia('(prefers-reduced-motion: reduce)').matches

    setIsClosing(true)
    closeTimerRef.current = window.setTimeout(
      () => {
        dialogRef.current?.close()
        after()
      },
      reduceMotion ? 0 : EXIT_DURATION_MS,
    )
  }

  function handleCancel() {
    if (!isClosing) {
      closeAnimated(onClose)
    }
  }

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()

    if (isClosing) {
      return
    }

    const trimmedName = name.trim()

    if (!trimmedName) {
      setNameError('Informe o nome do canal.')
      return
    }

    setNameError(undefined)
    setFormError(null)
    setIsSubmitting(true)

    try {
      const channel = await channelsApi.create({
        name: trimmedName,
        description: description.trim() || null,
      })

      // Same as cancel: animate out, restore focus, then the parent unmounts
      // the dialog.
      closeAnimated(() => onCreated(channel))
    } catch (error) {
      // ChannelsController answers 409 only for a duplicate name.
      setFormError(
        getErrorMessage(error, { 409: 'Já existe um canal com esse nome.' }),
      )
      setIsSubmitting(false)
    }
  }

  return (
    <dialog
      ref={dialogRef}
      className={isClosing ? 'modal modal-closing' : 'modal'}
      aria-labelledby="create-channel-title"
      onClose={onClose}
      onCancel={(event) => {
        // Esc: stays open while submitting, otherwise closes with the same
        // animation as Cancelar.
        event.preventDefault()

        if (!isSubmitting) {
          handleCancel()
        }
      }}
    >
      <form onSubmit={handleSubmit} noValidate aria-busy={isSubmitting}>
        <h2 id="create-channel-title">Criar canal</h2>

        {formError && (
          <Alert variant="error" role="alert">
            {formError}
          </Alert>
        )}

        <FormField
          id="channel-name"
          label="Nome"
          type="text"
          value={name}
          onChange={setName}
          autoComplete="off"
          error={nameError}
          maxLength={NAME_MAX_LENGTH}
        />

        <div className="field">
          <label htmlFor="channel-description">
            Descrição <span className="optional">(opcional)</span>
          </label>
          <textarea
            id="channel-description"
            name="channel-description"
            rows={3}
            maxLength={DESCRIPTION_MAX_LENGTH}
            value={description}
            onChange={(event) => setDescription(event.target.value)}
          />
        </div>

        <div className="modal-actions">
          <Button variant="secondary" onClick={handleCancel} disabled={isSubmitting}>
            Cancelar
          </Button>
          <Button type="submit" isLoading={isSubmitting}>
            {isSubmitting ? 'Criando…' : 'Criar canal'}
          </Button>
        </div>
      </form>

      {/* Last in the DOM so the dialog still opens with focus on the name
          field; placed in the top corner by CSS. */}
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

export default CreateChannelModal
