from datetime import date

from app import store
from app.models import Album, Artist


def _sample_album(title: str = "Test Title") -> Album:
    return Album(
        id=0,
        title=title,
        artist=Artist(name="Test Artist", birthdate=date(2000, 1, 1), birth_place="Testville"),
        price=1.23,
        image_url="https://example.com/img.png",
        year=2025,
    )


def test_get_all_returns_seeded_albums():
    all_albums = store.get_all()
    assert len(all_albums) >= 6
    assert all(a.title for a in all_albums)


def test_get_by_id_returns_album_when_exists():
    album = store.get_by_id(1)
    assert album is not None
    assert album.id == 1


def test_get_by_id_returns_none_when_missing():
    assert store.get_by_id(10_000_000) is None


def test_get_by_year_filters():
    result = store.get_by_year(2022)
    assert len(result) >= 1
    assert all(a.year == 2022 for a in result)


def test_add_assigns_next_id_and_persists():
    created = store.add(_sample_album())
    try:
        assert created.id > 0
        assert store.get_by_id(created.id) is not None
    finally:
        store.delete(created.id)


def test_update_modifies_existing_album():
    created = store.add(_sample_album("Original"))
    try:
        updated = store.update(created.id, _sample_album("Updated"))
        assert updated is not None
        assert updated.id == created.id
        assert updated.title == "Updated"
    finally:
        store.delete(created.id)


def test_update_returns_none_when_missing():
    assert store.update(10_000_000, _sample_album()) is None


def test_delete_removes_album():
    created = store.add(_sample_album("Bye"))
    assert store.delete(created.id) is True
    assert store.get_by_id(created.id) is None
    assert store.delete(created.id) is False
