import { useEffect, type ReactNode } from 'react'
import { Icon } from './Icon'

interface ModalProps {
  children: ReactNode
  onClose: () => void
  subtitle?: string
  title: string
}

export function Modal({ children, onClose, subtitle, title }: ModalProps) {
  useEffect(() => {
    const closeOnEscape = (event: KeyboardEvent) => {
      if (event.key === 'Escape') onClose()
    }
    window.addEventListener('keydown', closeOnEscape)
    return () => window.removeEventListener('keydown', closeOnEscape)
  }, [onClose])

  return (
    <div aria-modal="true" className="modal-layer" role="dialog">
      <button aria-label="Close dialog" className="modal-backdrop" onClick={onClose} type="button" />
      <section className="modal-card">
        <header className="modal-header">
          <div><h2>{title}</h2>{subtitle && <p>{subtitle}</p>}</div>
          <button aria-label="Close" className="icon-button" onClick={onClose} type="button"><Icon name="close" /></button>
        </header>
        {children}
      </section>
    </div>
  )
}
