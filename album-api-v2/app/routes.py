from fastapi import APIRouter, HTTPException, Response, status

from . import store
from .models import Album, AlbumInput

router = APIRouter(prefix="/albums", tags=["albums"])


@router.get("", response_model=list[Album], response_model_by_alias=True)
def list_albums():
    return store.get_all()


@router.get("/{album_id}", response_model=Album, response_model_by_alias=True)
def get_album(album_id: int):
    album = store.get_by_id(album_id)
    if album is None:
        raise HTTPException(status_code=status.HTTP_404_NOT_FOUND)
    return album


@router.get("/year/{year}", response_model=list[Album], response_model_by_alias=True)
def get_albums_by_year(year: int):
    return store.get_by_year(year)


@router.post(
    "",
    response_model=Album,
    response_model_by_alias=True,
    status_code=status.HTTP_201_CREATED,
)
def create_album(payload: AlbumInput, response: Response):
    album = Album(**payload.model_dump(by_alias=False))
    created = store.add(album)
    response.headers["Location"] = f"/albums/{created.id}"
    return created


@router.put("/{album_id}", response_model=Album, response_model_by_alias=True)
def update_album(album_id: int, payload: AlbumInput):
    album = Album(**payload.model_dump(by_alias=False))
    updated = store.update(album_id, album)
    if updated is None:
        raise HTTPException(status_code=status.HTTP_404_NOT_FOUND)
    return updated


@router.delete("/{album_id}", status_code=status.HTTP_204_NO_CONTENT)
def delete_album(album_id: int):
    if not store.delete(album_id):
        raise HTTPException(status_code=status.HTTP_404_NOT_FOUND)
    return Response(status_code=status.HTTP_204_NO_CONTENT)
