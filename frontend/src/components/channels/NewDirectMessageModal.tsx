import { useEffect, useRef, useState, type FormEvent } from 'react'
import * as directMessagesApi from '../../api/directMessages'
import { getErrorMessage } from '../../api/errors'
import * as usersApi from '../../api/users'
import type { UserSearchResponse } from '../../types/auth'
import type { DirectMessageResponse } from '../../types/channel'
import FormField from '../FormField'
import { Alert } from '../ui/Alert'
import { Avatar } from '../ui/Avatar'
import { Button, IconButton } from '../ui/Button'
import { IconX, Spinner } from '../ui/icons'

// Column limit from GameHubDbContext (User.Username).
const USERNAME_MAX_LENGTH = 50

// Wait for a pause in typing before searching.
const SEARCH_DELAY_MS = 300

// Matches the .modal-closing animation in index.css.
const EXIT_DURATION_MS = 180

type SearchState =
  | { status: 'idle' }
  | { status: 'loading' }
  | { status: 'success'; users: UserSearchResponse[] }
  | { status: 'error'; message: string }

interface NewDirectMessageModalProps {
  onClose: () => void
  // The conversation with the chosen user: the existing one, or a new one.
  onOpened: (directMessage: DirectMessageResponse) => void
}

function NewDirectMessageModal({ onClose, onOpened }: NewDirectMessageModalProps) {
  const dialogRef = useRef<HTMLDialogElement>(null)
  const [term, setTerm] = useState('')
  const [search, setSearch] = useState<SearchState>({ status: 'idle' })
  const [openingUserId, setOpeningUserId] = useState<string | null>(null)
  const [openError, setOpenError] = useState<string | null>(null)
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

  // Search as the user types. Every keystroke cancels the pending search
  // (timer and request), so only the latest term's results are shown.
  const trimmedTerm = term.trim()

  useEffect(() => {
    if (!trimmedTerm) {
      return
    }

    const controller = new AbortController()
    const timer = window.setTimeout(() => {
      setSearch({ status: 'loading' })

      usersApi
        .search(trimmedTerm, controller.signal)
        .then((users) => {
          if (!controller.signal.aborted) {
            setSearch({ status: 'success', users })
          }
        })
        .catch((err: unknown) => {
          if (!controller.signal.aborted) {
            setSearch({ status: 'error', message: getErrorMessage(err) })
          }
        })
    }, SEARCH_DELAY_MS)

    return () => {
      window.clearTimeout(timer)
      controller.abort()
    }
  }, [trimmedTerm])

  function handleTermChange(value: string) {
    setTerm(value)
    setOpenError(null)

    if (!value.trim()) {
      setSearch({ status: 'idle' })
    }
  }

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

  async function openWith(user: UserSearchResponse) {
    if (openingUserId || isClosing) {
      return
    }

    setOpenError(null)
    setOpeningUserId(user.id)

    try {
      const directMessage = await directMessagesApi.open({ userId: user.id })

      closeAnimated(() => onOpened(directMessage))
    } catch (error) {
      setOpenError(getErrorMessage(error, { 404: 'Esse usuário não existe mais.' }))
      setOpeningUserId(null)
    }
  }

  // Enter in the field picks the first result.
  function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()

    if (search.status === 'success' && search.users.length > 0) {
      void openWith(search.users[0])
    }
  }

  function renderResults() {
    switch (search.status) {
      case 'idle':
        return null
      case 'loading':
        return (
          <p className="user-search-status" role="status">
            <Spinner />
            Buscando…
          </p>
        )
      case 'error':
        return (
          <Alert variant="error" role="alert">
            {search.message}
          </Alert>
        )
      case 'success':
        if (search.users.length === 0) {
          return (
            <p className="user-search-status" role="status">
              Nenhum usuário encontrado.
            </p>
          )
        }

        return (
          <>
            <p className="visually-hidden" role="status">
              {search.users.length === 1
                ? '1 usuário encontrado.'
                : `${search.users.length} usuários encontrados.`}
            </p>
            <ul className="user-results" aria-label="Usuários encontrados">
              {search.users.map((user) => (
                <li key={user.id}>
                  <button
                    type="button"
                    className="user-result"
                    disabled={openingUserId !== null}
                    aria-busy={openingUserId === user.id || undefined}
                    onClick={() => void openWith(user)}
                  >
                    <Avatar name={user.username} size={28} />
                    <span className="user-result-name">{user.username}</span>
                    {openingUserId === user.id && <Spinner />}
                  </button>
                </li>
              ))}
            </ul>
          </>
        )
    }
  }

  return (
    <dialog
      ref={dialogRef}
      className={isClosing ? 'modal modal-closing' : 'modal'}
      aria-labelledby="new-dm-title"
      aria-describedby="new-dm-description"
      onClose={onClose}
      onCancel={(event) => {
        event.preventDefault()

        if (!openingUserId) {
          handleCancel()
        }
      }}
    >
      <form onSubmit={handleSubmit} noValidate aria-busy={openingUserId !== null}>
        <div className="modal-heading">
          <h2 id="new-dm-title">Nova mensagem</h2>
          <p id="new-dm-description" className="modal-subtitle">
            Procure alguém pelo nome de usuário para conversar em particular.
          </p>
        </div>

        {openError && (
          <Alert variant="error" role="alert">
            {openError}
          </Alert>
        )}

        <FormField
          id="dm-search"
          label="Nome de usuário"
          type="text"
          value={term}
          onChange={handleTermChange}
          autoComplete="off"
          maxLength={USERNAME_MAX_LENGTH}
        />

        <div className="user-search-results">{renderResults()}</div>

        <div className="modal-actions">
          <Button variant="secondary" onClick={handleCancel} disabled={openingUserId !== null}>
            Cancelar
          </Button>
        </div>
      </form>

      <IconButton
        label="Fechar"
        icon={<IconX />}
        size="sm"
        className="modal-close"
        onClick={handleCancel}
        disabled={openingUserId !== null}
      />
    </dialog>
  )
}

export default NewDirectMessageModal
