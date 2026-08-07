# Single hinged door art drop

Replace the bytes of the four PNG files in this folder and keep every
existing `.meta` file unchanged.

The two files for each slope must use the same transparent 56 x 286 pixel
canvas. Keep the closed door and frame registered to exactly the same point;
the prepared import metadata aligns both layers with one wall panel.

- `SingleHingedDoor_RisingLeft_Frame.png` — stationary frame only
- `SingleHingedDoor_RisingLeft_Door.png` — moving door panel only
- `SingleHingedDoor_RisingRight_Frame.png` — stationary frame only
- `SingleHingedDoor_RisingRight_Door.png` — moving door panel only

In the default North view, the rising (upper) endpoint of each panel is its
hinge. That physical hinge stays fixed as the camera rotates. When opened, the
frame stays on the home wall while the door panel switches to its perpendicular
logical edge and uses the projected opposite-slope door sprite. Leave
transparent breathing room around the art, but do not move the shared canvas
registration between the frame and door files.

The construction-picker icon has its own replace-in-place slot at:

`Assets/Art/UI/Construction/Doors/SingleHingedDoorIcon.png`
