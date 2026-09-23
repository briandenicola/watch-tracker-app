import { mount } from '@vue/test-utils'
import { describe, expect, it } from 'vitest'
import HelpPage from './HelpPage.vue'
import { watchHelpSections } from '@/constants/watchHelp'

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
})
