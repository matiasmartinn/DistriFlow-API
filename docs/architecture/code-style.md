# Code Style

Prefer:

- explicit code
- readability
- direct flow
- consistency

Avoid:

- helper explosion
- abstraction chains
- overengineering
- defensive programming excess

---

## Method Style

Prefer:

- small methods
- explicit names
- straightforward logic

Avoid:

- deeply nested logic
- generic utility wrappers
- hidden behavior

---

## Abstractions

Do not introduce abstractions for hypothetical future reuse.

Only abstract when duplication becomes real and repeated.

---

## Minimal Diff

Change only what is required by the task.

Do not:

- refactor unrelated files
- rename unrelated symbols
- modify architecture while implementing a simple endpoint
- add dependencies without explicit request
