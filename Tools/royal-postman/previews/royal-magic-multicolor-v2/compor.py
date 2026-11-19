"""Compose the comparison and encode its animated version from Blender frames."""

import argparse
import os
import subprocess
from pathlib import Path

import imageio_ffmpeg
from PIL import Image, ImageChops, ImageDraw, ImageFont, ImageStat


SIZE = (1120, 784)
FRAME_COUNT = 108
FPS = 24
SOURCE_SIZE = (1152, 540)
BOARD_CELL = 40
SPRITE_SIZE = 34
CONCEPTS = [
    ("Cubo do Caos", "Fileira e coluna giram 90°", "cubo-do-caos.png", 286, 277),
    ("Talismã dos Elementos", "Núcleo fixo, coroas em sentidos opostos", "talisma-dos-elementos.png", 834, 875),
]


def parse_args():
    parser = argparse.ArgumentParser()
    parser.add_argument("--still", type=Path, required=True)
    parser.add_argument("--frames", type=Path, required=True)
    parser.add_argument("--board", type=Path, required=True)
    return parser.parse_args()


def make_board(source):
    image = Image.open(source).convert("RGB")
    crop = image.crop((135, 36, 828, 531))
    draw = ImageDraw.Draw(crop)
    draw.rectangle((303, 204, 390, 291), fill=crop.getpixel((302, 203)))
    return crop.resize((BOARD_CELL * 7, BOARD_CELL * 5), Image.Resampling.LANCZOS).convert("RGBA")


def background():
    canvas = Image.new("RGBA", SIZE, "#181321")
    wash = Image.new("RGBA", (140, 98))
    pixels = []
    for y in range(wash.height):
        for x in range(wash.width):
            distance = ((x - 69.5) / 77) ** 2 + (y / 84) ** 2
            alpha = round(91 * (1 - min(distance, 1)) ** 2)
            pixels.append((103, 53, 129, alpha))
    wash.putdata(pixels)
    canvas.alpha_composite(wash.resize(SIZE, Image.Resampling.BILINEAR))
    return canvas


def font(name, size):
    return ImageFont.truetype(str(Path(os.environ["WINDIR"]) / "Fonts" / name), size)


def layout(board):
    canvas = background()
    draw = ImageDraw.Draw(canvas)
    ivory, muted, gold = "#FFF0D8", "#D6C7E1", "#F4CD90"
    draw.text((560, 33), "Royal Magic", font=font("georgiab.ttf", 43), fill=ivory, anchor="mt")
    draw.text((560, 84), "Especial multicolorido", font=font("segoeui.ttf", 17), fill=muted, anchor="mt")
    for title, motion, _, center, _ in CONCEPTS:
        draw.rounded_rectangle((center - 254, 119, center + 254, 752), 22, fill="#242032", outline="#453950")
        draw.text((center, 144), title, font=font("georgiab.ttf", 28), fill=ivory, anchor="mt")
        draw.text((center, 178), motion, font=font("segoeui.ttf", 16), fill=muted, anchor="mt")
        draw.text((center, 497), "NO TABULEIRO  ·  CÉLULA DE 40 PX",
                  font=font("segoeuib.ttf", 12), fill=gold, anchor="mt")
        draw.rounded_rectangle((center - 146, 520, center + 146, 732), 8, fill="#342B42", outline="#4A3D57")
        canvas.alpha_composite(board, (center - board.width // 2, 526))
    return canvas


def split(source):
    assert source.size == SOURCE_SIZE
    midpoint = source.width // 2
    return [source.crop((0, 0, midpoint, source.height)), source.crop((midpoint, 0, source.width, source.height))]


def draw_piece(canvas, piece, anchor, dest, scale):
    box = piece.getbbox(alpha_only=True)
    if box is None:
        raise ValueError("Empty render")
    sprite = piece.crop(box)
    size = (max(1, round(sprite.width * scale)), max(1, round(sprite.height * scale)))
    pos = (round(dest[0] + (box[0] - anchor[0]) * scale), round(dest[1] + (box[1] - anchor[1]) * scale))
    canvas.alpha_composite(sprite.resize(size, Image.Resampling.LANCZOS), pos)


def compare(base, render, board_scales):
    canvas = base.copy()
    for i, (_, _, _, center, source_x) in enumerate(CONCEPTS):
        piece = render[i]
        anchor = (source_x - i * piece.width, piece.height // 2)
        draw_piece(canvas, piece, anchor, (center, 348), 0.59)
        draw_piece(canvas, piece, anchor, (center, 626), board_scales[i])
    return canvas


def check_comparison(image):
    regions = [image.crop((center - 140, 526, center + 140, 726)) for _, _, _, center, _ in CONCEPTS]
    a, b = [region.tobytes() for region in regions]
    for index, (left, right) in enumerate(zip(a, b)):
        if left != right:
            y, x = divmod(index // 4, 280)
            assert 120 <= x < 160 and 80 <= y < 120, (x, y)
    assert a != b


def encode(base, paths, board_scales, destination):
    ffmpeg = imageio_ffmpeg.get_ffmpeg_exe()
    encoder = subprocess.Popen([
        ffmpeg, "-v", "error", "-y", "-f", "rawvideo", "-pixel_format", "rgb24",
        "-video_size", f"{SIZE[0]}x{SIZE[1]}", "-framerate", str(FPS), "-i", "pipe:0",
        "-c:v", "libx264", "-preset", "medium", "-crf", "17",
        "-pix_fmt", "yuv420p", "-movflags", "+faststart", str(destination),
    ], stdin=subprocess.PIPE)
    with encoder:
        with encoder.stdin:
            for path in paths:
                with Image.open(path) as native:
                    frame = compare(base, split(native.convert("RGBA")), board_scales).convert("RGB")
                    encoder.stdin.write(frame.tobytes())
    if encoder.returncode:
        raise subprocess.CalledProcessError(encoder.returncode, encoder.args)


def main():
    args = parse_args()
    out = Path(__file__).resolve().parent
    base = layout(make_board(args.board))
    with Image.open(args.still) as native:
        still = split(native.convert("RGBA"))
    board_scales = []
    for piece, (_, _, sprite_name, _, _) in zip(still, CONCEPTS):
        box = piece.getbbox(alpha_only=True)
        assert box is not None
        sprite = piece.crop(box)
        padded = Image.new("RGBA", (sprite.width + 8, sprite.height + 8))
        padded.alpha_composite(sprite, (4, 4))
        padded.save(out / sprite_name)
        board_scales.append(SPRITE_SIZE / max(sprite.size))

    comparison = compare(base, still, board_scales)
    check_comparison(comparison)
    comparison.convert("RGB").save(out / "comparativo-no-tabuleiro.png", optimize=True)
    paths = sorted(args.frames.glob("frame-*.png"))
    assert len(paths) == FRAME_COUNT, len(paths)
    with Image.open(paths[0]) as first, Image.open(paths[-1]) as last, Image.open(paths[39]) as middle:
        diff = ImageChops.difference(first, last)
        assert diff.getchannel("A").getbbox() is None, "Loop end changed the silhouette"
        assert max(ImageStat.Stat(diff).mean) < 0.01, "Loop end changed the resting pose"
        assert first.tobytes() != middle.tobytes(), "Missing movement"
    paths[-1] = paths[0]
    encode(base, paths, board_scales, out / "previa-em-movimento.mp4")
    print(f"PASS | {FRAME_COUNT} Blender frames, {FPS} fps, exact loop, same board outside the middle cell")


if __name__ == "__main__":
    main()
