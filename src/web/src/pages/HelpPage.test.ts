import { mount } from '@vue/test-utils'
import { describe, expect, it } from 'vitest'
import HelpPage from './HelpPage.vue'
import { watchAnatomyParts, watchHelpSections } from '@/constants/watchHelp'

describe('HelpPage', () => {
  it('explains every configured watch-field section in plain language', () => {
    const wrapper = mount(HelpPage)

    expect(wrapper.get('h1').text()).toBe('Watch Field Guide')
    for (const section of watchHelpSections) {
      expect(wrapper.text()).toContain(section.title)
      for (const term of section.terms) expect(wrapper.text()).toContain(term.name)
    }
    expect(wrapper.text()).toContain('full distance from the tip of the upper lugs')
    expect(wrapper.text()).toContain('The ring around the crystal')
    expect(wrapper.text()).toContain('The small knob')
  })

  it('shows a color-coded watch anatomy diagram with a matching legend', async () => {
    const wrapper = mount(HelpPage)

    expect(wrapper.text()).toContain('Anatomy of a Watch')
    expect(wrapper.find('[data-testid="watch-anatomy-svg"]').exists()).toBe(true)
    for (const part of watchAnatomyParts) {
      const button = wrapper.get(`button[data-part="${part.key}"]`)
      expect(button.text()).toContain(part.name)
      expect(button.find('span[aria-hidden="true"]').attributes('style')).toContain('background-color')
    }

    const crown = wrapper.get('button[data-part="crown"]')
    await crown.trigger('click')
    expect(crown.attributes('aria-pressed')).toBe('true')
  })
})
