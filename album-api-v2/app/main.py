from fastapi import FastAPI

from .routes import router as albums_router

app = FastAPI(title="albums-api (v2, FastAPI)")
app.include_router(albums_router)


@app.get("/healthz", include_in_schema=False)
def healthz():
    return {"status": "ok"}


if __name__ == "__main__":
    import uvicorn

    uvicorn.run("app.main:app", host="0.0.0.0", port=3000)
