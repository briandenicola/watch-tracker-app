import DOMPurify from 'dompurify'
import { marked } from 'marked'

const allowedTags = [
  'a', 'blockquote', 'br', 'code', 'em', 'h1', 'h2', 'h3', 'h4',
  'hr', 'li', 'ol', 'p', 'pre', 'strong', 'ul',
]

/**
 * Renders user-, AI-, and provider-supplied Markdown with the same narrow HTML
 * allow-list everywhere it is displayed.
 */
export function renderMarkdown(text: string): string {
  const raw = marked.parse(text, { async: false }) as string
  const sanitized = DOMPurify.sanitize(raw, {
    ALLOWED_TAGS: allowedTags,
    ALLOWED_ATTR: ['href'],
    ALLOWED_URI_REGEXP: /^https?:/i,
  })
  const document = new DOMParser().parseFromString(sanitized, 'text/html')
  for (const link of Array.from(document.links)) {
    link.target = '_blank'
    link.rel = 'noopener noreferrer'
  }
  return document.body.innerHTML
}
