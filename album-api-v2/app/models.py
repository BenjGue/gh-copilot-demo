from datetime import date
from typing import Optional

from pydantic import BaseModel, ConfigDict, Field
from pydantic.alias_generators import to_camel


class _CamelModel(BaseModel):
    """Base model that serializes/deserializes using camelCase to match the
    JSON shape produced by ASP.NET Core's default System.Text.Json options."""

    model_config = ConfigDict(
        alias_generator=to_camel,
        populate_by_name=True,
    )


class Artist(_CamelModel):
    name: str
    birthdate: date
    birth_place: str


class Album(_CamelModel):
    # Field name mirrors the .NET record property "Image_url" — keep snake_case
    # in the wire format on purpose so the viewer keeps receiving `image_url`.
    id: int = 0
    title: str
    artist: Artist
    price: float
    image_url: str = Field(serialization_alias="image_url", validation_alias="image_url")
    year: int


class AlbumInput(_CamelModel):
    """Payload accepted by POST/PUT. Id is ignored/overridden by the store."""

    id: Optional[int] = 0
    title: str
    artist: Artist
    price: float
    image_url: str = Field(serialization_alias="image_url", validation_alias="image_url")
    year: int
