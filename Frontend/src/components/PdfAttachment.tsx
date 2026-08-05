import { useState } from 'react'
import { downloadSubmissionPdf } from '../lib/api'
import { Icon } from './Icon'

interface PdfAttachmentProps {
  fileName: string
  fileSize: number
  submissionId: string
  token: string
}

function formatFileSize(bytes: number) {
  return bytes < 1024 * 1024
    ? `${Math.max(1, Math.round(bytes / 1024))} KB`
    : `${(bytes / (1024 * 1024)).toFixed(1)} MB`
}

export function PdfAttachment({ fileName, fileSize, submissionId, token }: PdfAttachmentProps) {
  const [downloading, setDownloading] = useState(false)
  const [error, setError] = useState('')

  const download = async () => {
    setDownloading(true)
    setError('')
    try {
      await downloadSubmissionPdf(submissionId, fileName, token)
    } catch {
      setError('Download failed. Please try again.')
    } finally {
      setDownloading(false)
    }
  }

  return (
    <div className="pdf-attachment">
      <span className="pdf-badge">PDF</span>
      <span><strong>{fileName}</strong><small>{formatFileSize(fileSize)}</small></span>
      <button aria-label={`Download ${fileName}`} disabled={downloading} onClick={download} type="button"><Icon name="download" size={17} /> {downloading ? 'Downloading…' : 'Download'}</button>
      {error && <small className="pdf-error" role="alert">{error}</small>}
    </div>
  )
}
