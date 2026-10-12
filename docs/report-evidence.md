# Report Evidence Log

Each member's report is **individual**. Use this file to collect evidence as you build, so
Chapters 5–7 aren't written from memory at the end. Add entries under your own name.

## Chapter checklist
| Chapter | Evidence to collect while building |
|---|---|
| 3 Project management | Branch list, own commit history, merged PR, contributors graph, sprint board |
| 4 Design | Use case, class, ER diagrams (shared) · own sequence diagram |
| 5 Development | Screenshot of each page you built · short code excerpts of key logic |
| 6 Quality assurance | Test cases (ID, steps, expected, actual, pass/fail) · screenshots of validation and error handling |
| 7 Reflection | Problems hit and how they were solved · what you'd improve |

## AI use declaration (required by the brief)
Each report must state which AI tools were used and for what. Keep a note of how you used them.

---

## Jordan — Domain C

| Date | Item | Evidence / note |
|---|---|---|
| 2026-10-11 | Layout shell (Sprint 1) | See the detailed entry below |

### 2026-10-11 — Layout shell (`feature/layout-shell`)

**Ch.5 screenshots to capture**
1. Home page at desktop width — navbar, hero, opening hours, footer in one frame.
2. Home page at phone width (F12, then Ctrl+Shift+M for the device toolbar) — collapsed
   hamburger menu, then the same with the menu open.
3. The navbar with the active-page underline visible on Home, for the "user always knows where
   they are" point.
4. Keyboard focus ring — press Tab until the Privacy link is focused, screenshot the plum outline.
   Good evidence for the accessibility paragraph.
5. `site.css` design-token block (`:root`) — a short code excerpt showing one place controls the
   whole palette.

**Ch.6 test cases**

| ID | Steps | Expected | Actual |
|---|---|---|---|
| TC-UI-01 | Open the site at 1440px, then drag the window down to 375px | Navbar collapses to a hamburger below 768px; menu opens and closes on click; no horizontal scrollbar at any width | |
| TC-UI-02 | Open any page and press Tab repeatedly | Every link and button shows a visible plum focus ring in reading order; nothing is reachable but invisible | |

**Ch.7 reflection notes**
- *CSS isolation does not reach tag-helper output.* The navbar styles were first written in
  `_Layout.cshtml.css`. The build succeeded and the scoped stylesheet was served, but the wordmark
  and the Home link were unstyled: CSS isolation works by stamping a `b-xxxxx` attribute on each
  element and rewriting selectors to match, and elements rendered by a tag helper
  (`<a asp-page="...">`) come out without that attribute. Found it by reading the served HTML
  rather than by trusting the clean build. Fixed by moving the shell styles into the global
  `site.css`, nested under `.rr-header` / `.rr-footer` so they cannot leak. Worth a paragraph:
  a successful build is not evidence that the page is correct.
- *Linking to pages that do not exist.* An `asp-page` tag helper pointing at a missing page throws
  at render time, so the Menu / Book a table / My orders links are inert `<span>`s with the
  replacement line written in a comment beside each.
- *Working around a shared file.* `_Layout.cshtml` belongs to the whole group, so the restaurant
  name is a single `const` and the `_LoginPartial` slot is pre-cut — Carynn's Identity work drops
  into it without touching the rest of the file.

**AI use declaration note:** Claude Code was used to survey the lecturer's lab projects for
conventions, to draft the layout markup and stylesheet, and to diagnose the CSS isolation problem.
Design direction, the decision to cap the content width and all review of the output were mine.

## Wallace Chan — Domain B

| Date | Item | Evidence / note |
|---|---|---|
| | | |

## Carynn — Domain A

| Date | Item | Evidence / note |
|---|---|---|
| | | |
