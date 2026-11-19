"""Compare two Blender renders on the same board mockup."""

import argparse
import os
import subprocess
from pathlib import Path

import imageio_ffmpeg
from PIL import Image, ImageChops, ImageDraw, ImageFont


CELL = 40
TILE_INK = "#4B3B73"
CANVAS = (960, 590)
VIDEO = (864, 456)
BOARD = (
    ("yellow", "green", "red", "blue", "orange", "red", "green"),
    ("blue", "red", "green", "", "green", "yellow", "red"),
    ("orange", "green", "blue", "yellow", "red", "blue", "orange"),
)


def arguments():
    parser = argparse.ArgumentParser()
    parser.add_argument("--pieces", type=Path, required=True)
    parser.add_argument("--old", type=Path, required=True)
    parser.add_argument("--new", type=Path, required=True)
    parser.add_argument("--frames", type=Path)
    return parser.parse_args()


def label(draw, x, y, text, size, family="segoeui.ttf", ink="#F6E7F7"):
    font = ImageFont.truetype(str(Path(os.environ["WINDIR"]) / "Fonts" / family), size)
    draw.text((x, y), text, fill=ink, font=font, anchor="mt")


def load_image(path):
    with Image.open(path) as file:
        return file.convert("RGBA")


def fit(sprite, bounds, size):
    reference_span = max(bounds[2] - bounds[0], bounds[3] - bounds[1])
    scale = size / reference_span
    width, height = (round(length * scale) for length in sprite.size)
    return sprite.resize((width, height), Image.Resampling.LANCZOS)


def place(canvas, sprite, bounds, x, y, size):
    scaled = fit(sprite, bounds, size)
    center_x = (bounds[0] + bounds[2]) / 2 * scaled.width / sprite.width
    center_y = (bounds[1] + bounds[3]) / 2 * scaled.height / sprite.height
    canvas.alpha_composite(scaled, (round(x - center_x), round(y - center_y)))


def board_texture(directory):
    width = len(BOARD[0]) * CELL
    height = len(BOARD) * CELL
    result = Image.new("RGBA", (width, height), "#151024")
    draw = ImageDraw.Draw(result)
    colors = {name for row in BOARD for name in row if name}
    assets = {name: load_image(directory / f"{name}.png") for name in colors}
    boxes = {name: asset.getbbox(alpha_only=True) for name, asset in assets.items()}
    for j, row in enumerate(BOARD):
        for i, name in enumerate(row):
            x, y = i * CELL, j * CELL
            draw.rounded_rectangle((x + 1, y + 1, x + CELL - 2, y + CELL - 2), 7, fill=TILE_INK)
            if name:
                place(result, assets[name], boxes[name], x + CELL // 2, y + CELL // 2, 31)
    return result


def card(draw, center, top, width, height):
    draw.rounded_rectangle((center - width // 2, top, center + width // 2, top + height), 22,
                           fill="#272034", outline="#483953")


def compare(old, new, board):
    canvas = Image.new("RGBA", CANVAS, "#1C1528")
    draw = ImageDraw.Draw(canvas)
    label(draw, 480, 26, "Color Bomb", 40, "georgiab.ttf")
    label(draw, 480, 77, "Royal Magic", 17, ink="#D9C5E1")
    panels = (
        (242, "Primeiro render 3D", old),
        (718, "Novo acabamento 3D", new),
    )
    for center, title, sprite in panels:
        card(draw, center, 123, 442, 439)
        label(draw, center, 147, title, 23, "segoeuib.ttf")
        bounds = sprite.getbbox(alpha_only=True)
        assert bounds
        place(canvas, sprite, bounds, center, 286, 204)
        label(draw, center, 418, "MOCKUP DE TABULEIRO · CÉLULA DE 40 PX", 11, "segoeuib.ttf", "#E5D0ED")
        canvas.alpha_composite(board, (center - board.width // 2, 439))
        place(canvas, sprite, bounds, center, 439 + board.height // 2, 33)
    left = canvas.crop((102, 439, 382, 559))
    right = canvas.crop((578, 439, 858, 559))
    difference = ImageChops.difference(left, right).getbbox(alpha_only=False)
    assert difference and 120 <= difference[0] < difference[2] <= 160
    assert 40 <= difference[1] < difference[3] <= 80
    return canvas.convert("RGB")


def animation_slate(board):
    canvas = Image.new("RGBA", VIDEO, "#1C1528")
    draw = ImageDraw.Draw(canvas)
    label(draw, 432, 26, "Color Bomb em movimento", 33, "georgiab.ttf")
    card(draw, 432, 86, 800, 334)
    label(draw, 627, 155, "NO TABULEIRO · CÉLULA DE 40 PX", 11, "segoeuib.ttf", "#E5D0ED")
    canvas.alpha_composite(board, (627 - board.width // 2, 182))
    return canvas


def encode(paths, board, destination):
    assert len(paths) == 48
    bounds = load_image(paths[0]).getbbox(alpha_only=True)
    slate = animation_slate(board)
    ffmpeg = imageio_ffmpeg.get_ffmpeg_exe()
    args = [
        ffmpeg, "-v", "error", "-y", "-f", "rawvideo", "-pixel_format", "rgb24",
        "-video_size", f"{VIDEO[0]}x{VIDEO[1]}", "-framerate", "24", "-i", "pipe:0",
        "-c:v", "libx264", "-crf", "16", "-pix_fmt", "yuv420p",
        "-movflags", "+faststart", str(destination),
    ]
    with subprocess.Popen(args, stdin=subprocess.PIPE) as process:
        with process.stdin:
            for path in paths:
                sprite = load_image(path)
                canvas = slate.copy()
                place(canvas, sprite, bounds, 222, 250, 283)
                place(canvas, sprite, bounds, 627, 182 + board.height // 2, 33)
                process.stdin.write(canvas.convert("RGB").tobytes())
    if process.returncode:
        raise subprocess.CalledProcessError(process.returncode, args)
    subprocess.run([ffmpeg, "-v", "error", "-i", str(destination), "-f", "null", "NUL"], check=True)


def main():
    settings = arguments()
    out = Path(__file__).resolve().parent
    base = board_texture(settings.pieces)
    reference = load_image(settings.new)
    compare(load_image(settings.old), reference, base).save(out / "comparativo-no-tabuleiro.png", optimize=True)
    if settings.frames:
        paths = sorted(settings.frames.glob("frame-*.png"))
        encode(paths, base, out / "previa-contragiro.mp4")


if __name__ == "__main__":
    main()
