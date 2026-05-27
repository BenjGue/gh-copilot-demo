# album-api-v2

Python **FastAPI** rewrite of the original .NET `albums-api`. Serves the same
JSON contract on port **3000** so the existing Vue viewer (and its nginx
reverse-proxy on `/albums`) keeps working with no changes.

## Stack

- Python 3.12
- FastAPI 0.115 + Uvicorn 0.30
- Pydantic 2.9
- pytest 8.3 + httpx (for `TestClient`)

## Project layout

```
album-api-v2/
├── app/
│   ├── __init__.py
│   ├── main.py        # FastAPI app, /healthz, uvicorn entrypoint
│   ├── models.py      # Pydantic Album + Artist (camelCase aliases)
│   ├── store.py       # Thread-safe in-memory store, seeded with 6 albums
│   └── routes.py      # /albums CRUD + /albums/year/{year}
├── tests/
│   ├── test_store.py
│   └── test_routes.py
├── requirements.txt
├── Dockerfile         # multi-stage: runs pytest in build, slim runtime
└── README.md
```

## HTTP contract

Base path: `/albums`

| Method | Path                  | Success      | Errors |
|--------|-----------------------|--------------|--------|
| GET    | `/albums`             | 200 + list   | —      |
| GET    | `/albums/{id}`        | 200 + album  | 404    |
| GET    | `/albums/year/{year}` | 200 + list   | —      |
| POST   | `/albums`             | 201 + album, `Location` header | — |
| PUT    | `/albums/{id}`        | 200 + album  | 404    |
| DELETE | `/albums/{id}`        | 204          | 404    |
| GET    | `/healthz`            | 200          | —      |

### JSON shape

Preserved verbatim from the .NET version so the viewer needs no changes:

```json
{
  "id": 1,
  "title": "You, Me and an App Id",
  "artist": {
    "name": "Daprize",
    "birthdate": "1985-04-12",
    "birthPlace": "Seattle, USA"
  },
  "price": 10.99,
  "image_url": "https://aka.ms/albums-daprlogo",
  "year": 2021
}
```

Note: `image_url` stays snake_case (legacy contract), while nested fields like
`birthPlace` use camelCase.

## Workshop usage (Dokploy)

This repo is **never run locally**. The validation path is:

```pwsh
git add .
git commit -m "..."
git push
```

Dokploy then:

1. Builds the image from [Dockerfile](Dockerfile).
2. Runs `python -m pytest -q` inside the build stage — a failing test fails the deploy.
3. Starts the runtime container on port `3000`.
4. The `album-viewer` nginx forwards `/albums` to `http://albums-api:3000`.

The compose service is still named `albums-api` (see
[`docker-compose.yml`](../docker-compose.yml)) so the existing reverse-proxy
contract is preserved — only the `build.context` was switched to
`./album-api-v2`.

## Verifying a deploy

After `git push` and a successful Dokploy build, on the generated
`*.traefik.me` URL:

- The viewer lists the same 6 albums as before.
- `GET /albums` returns a list of 6 items with the JSON shape above.
- `GET /albums/year/2022` returns 2 items.
- `GET /albums/9999` returns `404`.
- `GET /healthz` returns `{"status":"ok"}`.

## Tests

Tests live under [`tests/`](tests/) and run automatically inside the Docker
build. They cover:

- `store.py`: seed, get-by-id (hit/miss), get-by-year, add/update/delete.
- `routes.py`: status codes (200/201/204/404), wire format (snake_case
  `image_url`, nested `artist.birthPlace`), and `Location` header on create.
