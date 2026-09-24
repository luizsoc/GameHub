import { useEffect, useRef, useState, type FormEvent } from 'react'
import * as channelsApi from '../../api/channels'
import { getErrorMessage } from '../../api/errors'
import type { ChannelResponse } from '../../types/channel'
import FormField from '../FormField'

// Column limits from GameHubDbContext (Channel.Name / Channel.Description).
const NAME_MAX_LENGTH = 100
const DESCRIPTION_MAX_LENGTH = 500

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

  // Native <dialog> gives focus handling, Esc to close and a backdrop.
  // The open check keeps StrictMode's double effect from throwing.
  useEffect(() => {
    const dialog = dialogRef.current

    if (dialog && !dialog.open) {
      dialog.showModal()
    }
  }, [])

  // Native close() returns focus to the button that opened the dialog.
  // onClose() then unmounts it right away instead of waiting for the async
  // "close" event (which Chromium only delivers on the next rendered frame).
  function handleCancel() {
    dialogRef.current?.close()
    onClose()
  }

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()

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

      // Same as cancel: restore focus, then the parent unmounts the dialog.
      dialogRef.current?.close()
      onCreated(channel)
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
      className="modal"
      aria-labelledby="create-channel-title"
      onClose={onClose}
      onCancel={(event) => {
        if (isSubmitting) {
          event.preventDefault()
        }
      }}
    >
      <form onSubmit={handleSubmit} noValidate aria-busy={isSubmitting}>
        <h2 id="create-channel-title">Criar canal</h2>

        {formError && (
          <p className="form-error" role="alert">
            {formError}
          </p>
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
          <button
            type="button"
            className="button-secondary"
            onClick={handleCancel}
            disabled={isSubmitting}
          >
            Cancelar
          </button>
          <button type="submit" disabled={isSubmitting}>
            {isSubmitting ? 'Criando…' : 'Criar canal'}
          </button>
        </div>
      </form>
    </dialog>
  )
}

export default CreateChannelModal
