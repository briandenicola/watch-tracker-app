import { describe, expect, it } from 'vitest'
import { renderMarkdown } from './markdown'

describe('renderMarkdown', () => {
  it('keeps safe Markdown links and removes executable HTML', () => {
    const html = renderMarkdown(
      '[Safe](https://example.test) <img src=x onerror="alert(1)"> ' +
      '<script>alert(1)</script> [Unsafe](javascript:alert(1))',
    )

    expect(html).toContain('href="https://example.test"')
    expect(html).toContain('target="_blank"')
    expect(html).toContain('rel="noopener noreferrer"')
    expect(html).not.toContain('<img')
    expect(html).not.toContain('<script')
    expect(html).not.toContain('onerror')
    expect(html).not.toContain('javascript:')
  })
})
