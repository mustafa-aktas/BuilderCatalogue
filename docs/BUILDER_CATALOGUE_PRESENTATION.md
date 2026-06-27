# Builder Catalogue — from Excel to a digital product

Source copy and speaker guidance for the accompanying PowerPoint deck.

## 1. Builder Catalogue

### From an Excel tool to a trusted digital service

Stakeholder and delivery overview.

**Speaker note:** Today, some of the logic sits in Excel. It answers one useful question: which sets a user can build from the pieces they already own. We want to preserve that value, make it easier to use and maintain, and create a foundation for new capabilities.

## 2. Stakeholder overview: today and tomorrow

### Today — Excel

- Compares one user’s inventory with set requirements
- Finds sets the user can build with the correct pieces, colours, and quantities
- Depends on workbook logic and manual operation
- Is difficult to share, explain, govern, and extend

### Tomorrow — Builder Catalogue

- A simple service users can access consistently
- Uses catalogue and inventory data directly
- Explains buildable sets and missing pieces
- Creates a safe foundation for collaboration, custom builds, and colour flexibility

**The important point:** this is not a spreadsheet conversion. It is the creation of a supported product around a valuable business capability.

## 3. Why replace Excel?

| Excel constraint | Product response |
|---|---|
| Logic is hidden in formulas, macros, and manual steps | Rules become explicit, reviewed, and tested |
| Copies can contain different logic or stale data | One service and one current data source |
| Results are difficult to explain and audit | Show the pieces and quantities behind each answer |
| Changes are risky and knowledge may sit with one person | Version control, ownership, automated checks |
| Adding new scenarios increases workbook complexity | Reusable foundations support one feature at a time |
| Limited operational visibility | Monitoring, support, and usage measures |

**Speaker note:** Excel is not “bad”; it proved the need. The issue is that the capability now needs reliability, shared ownership, and room to grow.

## 4. What we gain

### For users

Faster answers, a clearer experience, and explanations they can trust.

### For the business

One governed rule set, better visibility of adoption, and less dependency on individuals.

### For the team

Smaller and safer changes, automated evidence, and a platform that can be extended without redesigning the workbook.

### Future value

Help users collaborate, design builds shared by a community, and use more of their inventory through controlled colour substitution.

**Guardrail:** the first release should replace the current Excel outcome before expanding its scope.

## 5. Replace the workbook without losing its knowledge

1. **Discover** — identify workbook owners, formulas, macros, hidden sheets, inputs, outputs, and manual workarounds.
2. **Define** — write each intended rule as a plain-language example, including edge cases.
3. **Build** — create the smallest service that answers the same core question.
4. **Prove** — run Excel and the service against the same “golden” examples and reconcile every difference.
5. **Adopt** — pilot with users, migrate data, provide support, and make Excel read-only only after exit criteria pass.
6. **Retire** — preserve an auditable archive and remove operational dependency on the workbook.

**Reconciliation rule:** every difference is classified as a defect, an approved rule change, or a data-quality problem. A named business rule owner signs it off.

## 6. Developer brief: the main goal

### Required outcome

Answer: **Which sets can `brickfan35` build with their existing inventory?**

### Business rule

A set is buildable only when the user owns every required:

- piece design;
- colour; and
- quantity.

### Thin end-to-end delivery

1. Read the user inventory and set catalogue.
2. Normalize them into one piece identity: design + colour + quantity.
3. Compare each set requirement with the owned inventory.
4. Return buildable sets and explain missing pieces for the rest.
5. Validate with approved Excel examples and automated tests.

### Done means

Agreed examples pass, results are explainable, failures are handled, and stakeholders can compare the answer with the old tool.

Challenge source: https://d30r5p5favh3z8.cloudfront.net/

## 7. Stretch outcomes: deliver independently after the main goal

### 1. Collaboration

For `landscape-artist` and `tropical-island`, find other users whose pieces cover the remaining shortfall.

**Approach:** calculate only the missing pieces, measure each user’s useful contribution, and return combinations without redundant collaborators.

### 2. Shared custom build

For `megabuilder99`, find the largest piece collection that at least 50% of other users can also build.

**Approach:** find the inventory shared by candidate groups, keep the minimum available quantity, and choose the most useful valid result.

### 3. Whole-colour substitution — hard

For `dr_crocodile`, find additional buildable sets when an entire colour group may move to a new colour not used elsewhere.

**Approach:** validate whole groups, assign unique replacement colours, and backtrack when one choice blocks another.

**Delivery rule:** each stretch is a separate backlog outcome. None is allowed to delay proving the main goal.

## 8. Build shared foundations once, add outcomes in order

### Shared foundation

- Catalogue and inventory access
- Consistent piece identity and input validation
- Explainable result models
- Automated examples from the Excel baseline
- Logging, monitoring, and error handling

### Delivery sequence

1. Main goal: exact buildability
2. Collaboration
3. Shared custom build
4. Whole-colour substitution
5. Production scaling and optimization as evidence requires

This sequence improves flow because every stage can be demonstrated, tested, and released without waiting for the complete vision.

**Technical boundaries to decide deliberately:** search size, response-time targets, caching and freshness, privacy, cancellation, and whether a result must be mathematically optimal or simply feasible.

## 9. Ways of working that improve flow

### One product goal and one ordered backlog

The product manager makes priority explicit. A new urgent request also identifies what moves down.

### Small vertical slices

Each item includes user experience, business rule, code, evidence, and release—not separate technical phases.

### Limit work in progress

Engineers pull new work only when capacity exists and swarm on blocked or nearly finished items.

### Decisions before development

The product manager, design lead, engineering lead, and relevant Excel rule owner agree examples and acceptance criteria before an item starts.

### Learn from delivery

Use fortnightly demonstrations and a decision log. Track cycle time, work-item age, blocked time, discrepancies, reliability, and adoption.

**Immediate decisions:** nominate Excel rule owners, approve the main-goal examples, and agree the parallel-run exit criteria.
