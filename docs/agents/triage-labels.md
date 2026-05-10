# Triage Labels

The skills speak in terms of canonical triage roles. This file maps those roles to the local workflow states used in this repo.

| Label in mattpocock/skills | Label in our tracker | Meaning |
| --- | --- | --- |
| `needs-triage` | `blocked` | Needs human evaluation before work can continue |
| `needs-info` | `blocked` | Waiting for missing information |
| `ready-for-agent` | `ready-for-agent` | Fully specified and ready for an agent |
| `ready-for-human` | `in-progress` | Needs human-led implementation or review |
| `wontfix` | `done` | Will not be actioned further |

Additional local workflow states:

| State | Meaning |
| --- | --- |
| `in-progress` | Work has started |
| `blocked` | Work cannot continue without a decision or dependency |
| `done` | Work is complete or closed |
