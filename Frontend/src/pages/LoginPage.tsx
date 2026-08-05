import { useState, type FormEvent } from 'react'
import { ApiError, login } from '../lib/api'
import type { AuthSession } from '../types'
import { Icon } from '../components/Icon'

interface LoginPageProps {
  onAuthenticated: (session: AuthSession) => void
}

export function LoginPage({ onAuthenticated }: LoginPageProps) {
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [error, setError] = useState('')
  const [submitting, setSubmitting] = useState(false)

  const submit = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault()
    setError('')
    setSubmitting(true)
    try {
      const session = await login(email, password)
      onAuthenticated(session)
    } catch (requestError) {
      setError(requestError instanceof ApiError ? requestError.message : 'Unable to reach the API.')
    } finally {
      setSubmitting(false)
    }
  }

  return (
    <main className="login-page">
      <section className="login-story">
        <div className="login-brand"><span className="brand-mark"><Icon name="school" size={25} /></span> Shikkha</div>
        <div className="story-copy">
          <span className="eyebrow light">Academic operations, simplified</span>
          <h1>One calm place to run your learning community.</h1>
          <p>Organize people, courses, subjects, assignments, and student progress from a focused admin workspace.</p>
        </div>
        <div className="story-stat"><strong>Clear oversight.</strong><span>Every academic workflow, connected.</span></div>
      </section>
      <section className="login-form-wrap">
        <form autoComplete="off" className="login-card" onSubmit={submit}>
          <div>
            <span className="eyebrow">Welcome back</span>
            <h2>Sign in to your workspace</h2>
            <p>Use your administrator, teacher, or student credentials to continue.</p>
          </div>
          {error && <div className="alert error" role="alert">{error}</div>}
          <label>Email address<input autoComplete="off" onChange={(event) => setEmail(event.target.value)} required type="email" value={email} /></label>
          <label>Password<input autoComplete="off" minLength={8} onChange={(event) => setPassword(event.target.value)} required type="password" value={password} /></label>
          <button className="primary-button wide" disabled={submitting} type="submit">
            {submitting ? 'Signing in…' : 'Sign in'} <Icon name="arrow" size={18} />
          </button>
        </form>
      </section>
    </main>
  )
}
