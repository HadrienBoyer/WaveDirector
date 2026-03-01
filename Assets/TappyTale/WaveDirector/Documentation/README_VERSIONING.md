# README_VERSIONING.md

Version: 1.0.0  
Last updated: 2026-03-01

## How to manage docs per release

Rule:
- Every code release MUST update docs and CHANGELOG in the same commit.

Recommended docs set:
- README.md (root overview)
- README_INSTALL.md
- README_WAVE_DESIGNER.md
- README_TIMELINE_EDITOR.md
- README_AI_DIRECTOR.md
- README_RUNTIME_API.md
- README_SAVE_ANALYTICS.md
- README_EXTENDING.md
- README_VERSIONING.md
- CHANGELOG.md

## What changes in each version

Patch release (x.y.Z)
- Bug fixes, small performance improvements
- Update:
  - CHANGELOG “Fixed”
  - Any README sections impacted by behavior change

Minor release (x.Y.z)
- New features, non-breaking API changes
- Update:
  - CHANGELOG “Added/Changed”
  - Relevant README(s) + examples
  - Include migration notes if any serialized data changes (even if non-breaking)

Major release (X.y.z)
- Breaking changes (API/serialized data/workflow)
- Update:
  - CHANGELOG “Breaking” with explicit migration steps
  - Add a “Migration” section in README_INSTALL.md

## Doc update checklist (per version)

- [ ] CHANGELOG updated with date and details
- [ ] README_INSTALL.md reflects any new setup requirements
- [ ] README_TIMELINE_EDITOR.md matches the current window interactions
- [ ] README_AI_DIRECTOR.md matches current tuning params
- [ ] README_SAVE_ANALYTICS.md matches file formats and paths
- [ ] README_EXTENDING.md updated with any new interfaces/events
- [ ] All menu paths correct
