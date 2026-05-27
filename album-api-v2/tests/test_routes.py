from fastapi.testclient import TestClient

from app.main import app

client = TestClient(app)


def _sample_payload(title: str = "Ctrl Album") -> dict:
    return {
        "id": 0,
        "title": title,
        "artist": {
            "name": "Ctrl Artist",
            "birthdate": "1995-03-14",
            "birthPlace": "Ctrl City",
        },
        "price": 11.11,
        "image_url": "https://example.com/c.png",
        "year": 2025,
    }


def test_list_albums_returns_200_with_seed():
    r = client.get("/albums")
    assert r.status_code == 200
    data = r.json()
    assert isinstance(data, list)
    assert len(data) >= 6
    # Wire format check: nested artist object + snake_case image_url
    first = data[0]
    assert set(["id", "title", "artist", "price", "image_url", "year"]).issubset(first.keys())
    assert isinstance(first["artist"], dict)
    assert set(["name", "birthdate", "birthPlace"]).issubset(first["artist"].keys())


def test_get_album_returns_200_when_found():
    r = client.get("/albums/1")
    assert r.status_code == 200
    assert r.json()["id"] == 1


def test_get_album_returns_404_when_missing():
    r = client.get("/albums/9999999")
    assert r.status_code == 404


def test_get_albums_by_year_filters():
    r = client.get("/albums/year/2023")
    assert r.status_code == 200
    data = r.json()
    assert all(a["year"] == 2023 for a in data)


def test_create_album_returns_201_with_new_id():
    r = client.post("/albums", json=_sample_payload())
    assert r.status_code == 201
    body = r.json()
    assert body["id"] > 0
    # Cleanup
    client.delete(f"/albums/{body['id']}")


def test_update_album_returns_200_when_found():
    created = client.post("/albums", json=_sample_payload("To Update")).json()
    try:
        r = client.put(f"/albums/{created['id']}", json=_sample_payload("Updated"))
        assert r.status_code == 200
        assert r.json()["title"] == "Updated"
    finally:
        client.delete(f"/albums/{created['id']}")


def test_update_album_returns_404_when_missing():
    r = client.put("/albums/9999999", json=_sample_payload())
    assert r.status_code == 404


def test_delete_album_returns_204_when_found():
    created = client.post("/albums", json=_sample_payload("To Delete")).json()
    r = client.delete(f"/albums/{created['id']}")
    assert r.status_code == 204


def test_delete_album_returns_404_when_missing():
    r = client.delete("/albums/9999999")
    assert r.status_code == 404
