# Blue enamel background and Incubadora title

Only the blue background finish from the V102 mock was approved. The V100/V101 tab designs were rejected. This change preserves all existing navigation, round button artwork, geometry and gameplay.

Three original built-in ImageGen PNGs in `Assets/RoyalAves/Art/UI/Menu/RoyalPostman/BlueEnamelV103/` provide the reflective navy background and the transparent Incubadora/Incubator titles. Native standalone Sprite assets crop the title alpha bounds without editing source pixels. The existing header language selection and size fitting remain in use. Native asset import verifies opacity, reflection contrast, transparent margins, serialization and lossless mipmapped imports. No runtime code changes are needed.

The installer modifies only the Background Image's sprite/tint, the header's PT/EN title references, and the displayed header sprite/size when it is the incubation section. All unrelated prefab components must remain semantically identical. Native checks, preservation audit and backups are archived in `production/review/blue-enamel-v103/`; exact generation prompts, sources and hashes are in `production/BLUE-ENAMEL-V103.json`.
