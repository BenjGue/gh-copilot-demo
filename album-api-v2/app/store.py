from datetime import date
from threading import Lock
from typing import List, Optional

from .models import Album, Artist

_lock = Lock()

_albums: List[Album] = [
    Album(
        id=1,
        title="You, Me and an App Id",
        artist=Artist(name="Daprize", birthdate=date(1985, 4, 12), birth_place="Seattle, USA"),
        price=10.99,
        image_url="https://aka.ms/albums-daprlogo",
        year=2021,
    ),
    Album(
        id=2,
        title="Seven Revision Army",
        artist=Artist(name="The Blue-Green Stripes", birthdate=date(1978, 9, 30), birth_place="Dublin, Ireland"),
        price=13.99,
        image_url="https://aka.ms/albums-containerappslogo",
        year=2022,
    ),
    Album(
        id=3,
        title="Scale It Up",
        artist=Artist(name="KEDA Club", birthdate=date(1990, 1, 22), birth_place="Amsterdam, Netherlands"),
        price=13.99,
        image_url="https://aka.ms/albums-kedalogo",
        year=2022,
    ),
    Album(
        id=4,
        title="Lost in Translation",
        artist=Artist(name="MegaDNS", birthdate=date(1982, 6, 5), birth_place="Tokyo, Japan"),
        price=12.99,
        image_url="https://aka.ms/albums-envoylogo",
        year=2023,
    ),
    Album(
        id=5,
        title="Lock Down Your Love",
        artist=Artist(name="V is for VNET", birthdate=date(1995, 11, 17), birth_place="Berlin, Germany"),
        price=12.99,
        image_url="https://aka.ms/albums-vnetlogo",
        year=2023,
    ),
    Album(
        id=6,
        title="Sweet Container O' Mine",
        artist=Artist(name="Guns N Probeses", birthdate=date(1988, 3, 8), birth_place="Los Angeles, USA"),
        price=14.99,
        image_url="https://aka.ms/albums-containerappslogo",
        year=2024,
    ),
]


def get_all() -> List[Album]:
    with _lock:
        return list(_albums)


def get_by_id(album_id: int) -> Optional[Album]:
    with _lock:
        return next((a for a in _albums if a.id == album_id), None)


def get_by_year(year: int) -> List[Album]:
    with _lock:
        return [a for a in _albums if a.year == year]


def add(album: Album) -> Album:
    with _lock:
        next_id = (max((a.id for a in _albums), default=0) + 1)
        created = album.model_copy(update={"id": next_id})
        _albums.append(created)
        return created


def update(album_id: int, album: Album) -> Optional[Album]:
    with _lock:
        for i, existing in enumerate(_albums):
            if existing.id == album_id:
                updated = album.model_copy(update={"id": album_id})
                _albums[i] = updated
                return updated
        return None


def delete(album_id: int) -> bool:
    with _lock:
        for i, existing in enumerate(_albums):
            if existing.id == album_id:
                del _albums[i]
                return True
        return False
