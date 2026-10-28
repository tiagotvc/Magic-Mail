# Hatchery controls V88

Requested layout: a smaller Cancel control to the left of Incubate, a smaller `>>` accelerator to its right, and one centered digital duration display below them.

The actual `Pets.prefab` uses native editable UI objects under `Pets/Hatchery`:

- `Cancel`: 144px round control, user-authored red center/gold border and existing close X icon.
- `Incubate`: existing 216px control and paw art retained.
- `Accelerate`: 144px round control using the same user PNG layers, with an independent native cream/brown `>>` text icon under its moving center. Label and quantity/next gold cost remain below it.
- `DurationDisplay`: centered at x=0, 760px wide, original digital clock retained.
- `AcceleratorDisplay`: previous lower module is inactive and retained for recovery; its old button is no longer wired.

The fixed rim and moving red center remain independent. The smaller controls scale the existing child geometry proportionally. No new PNGs or button painting were generated. Header, tabs, chamber, footer and profile are preserved.

Cancel only applies to an unfinished incubation. It returns exactly the original egg to inventory, clears that slot's timer/purchase tier, saves once, and stops its particles. Previously spent accelerator items and currency are not returned. Ready jobs and other slots are preserved. Repeated clicks cannot refund multiple eggs. Existing item/gold acceleration rules remain unchanged.

Build/review/merge scripts are in `production/*hatchery-v88*` and `HatcheryV88*.cs`. They stage the current authored prefab into the isolated native Unity project and install only the requested fields plus two new control subtrees. The merger rejects concurrent edits to affected fields and preserves every other existing component. Native checks and installation audit are archived under `production/review/hatchery-v88/` after installation. No interaction with the user's main Unity window is needed.
