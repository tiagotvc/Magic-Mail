# Glossy return marker V99

Replaces the offscreen return control's flat procedural frame with an original ImageGen sprite, `Assets/RoyalAves/Resources/MapNavigationV99/return-pin.png`. The purple enamel has sculpted highlights and a polished gold bevel. The portrait aperture is transparent; the live default/profile avatar is separately circularly masked beneath the frame, preserving player selection. The frame renders after the portrait so the inner gold bezel covers its edge.

The marker retains its 156 × 190 reference size, touch target and location above the bottom navigation. It still appears only away from the current playable level and smoothly returns there on tap, hiding upon arrival and behind menus. The on-node position card and level glow are unchanged.

Exact built-in ImageGen prompt and original source path are in `return-pin-v99-prompt.txt` and `return-pin-v99-manifest.json`. Install with `node Tools/level-map/install-return-pin-v99.cjs`; the unmodified generated PNG and stable metadata are preserved. No player saves or authored island placements are touched.

`ReturnPinV99Validation.Run` exercises native standard/narrow phone rendering, default/custom portraits, one raycast target, sprite assignment and overlay ordering, forward/reverse scrolling, actual monotonic eased return, arrival hiding, menu cover/restoration and completed-campaign hiding in the isolated Unity project. Captures are in `previews/return-pin-v99/`, report in `return-pin-v99-validation.txt`.
