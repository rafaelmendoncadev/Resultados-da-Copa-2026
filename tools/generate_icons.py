"""Generate Android mipmap icons from source PNG."""
from pathlib import Path
from PIL import Image

ROOT = Path(__file__).resolve().parent.parent
SOURCE = ROOT / "tools" / "appicon_source.png"
RES = ROOT / "Resources"

LEGACY_SIZES = {
    "mipmap-mdpi": 48,
    "mipmap-hdpi": 72,
    "mipmap-xhdpi": 96,
    "mipmap-xxhdpi": 144,
    "mipmap-xxxhdpi": 192,
}

FOREGROUND_SIZES = {
    "mipmap-mdpi": 108,
    "mipmap-hdpi": 162,
    "mipmap-xhdpi": 216,
    "mipmap-xxhdpi": 324,
    "mipmap-xxxhdpi": 432,
}


def save_square(img: Image.Image, size: int, path: Path) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    resized = img.resize((size, size), Image.Resampling.LANCZOS)
    resized.save(path, "PNG", optimize=True)
    print(f"  {path.relative_to(ROOT)} ({size}x{size})")


def load_square_source(path: Path) -> Image.Image:
    img = Image.open(path).convert("RGBA")
    side = min(img.size)
    left = (img.width - side) // 2
    top = (img.height - side) // 2
    return img.crop((left, top, left + side, top + side))


def main() -> None:
    img = load_square_source(SOURCE)
    print(f"Source: {SOURCE} (cropped to {img.size[0]}x{img.size[1]})")

    for folder, size in LEGACY_SIZES.items():
        save_square(img, size, RES / folder / "appicon.png")

    for folder, size in FOREGROUND_SIZES.items():
        save_square(img, size, RES / folder / "appicon_foreground.png")

    print("Done.")


if __name__ == "__main__":
    main()
