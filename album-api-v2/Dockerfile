# syntax=docker/dockerfile:1

# --- Build / test stage -------------------------------------------------------
FROM python:3.12-slim AS build
WORKDIR /src

ENV PIP_DISABLE_PIP_VERSION_CHECK=1 \
    PIP_NO_CACHE_DIR=1 \
    PYTHONDONTWRITEBYTECODE=1 \
    PYTHONUNBUFFERED=1

COPY requirements.txt ./
RUN pip install -r requirements.txt

COPY . ./

# Run unit tests — a failing test fails the Docker build (and the Dokploy deploy)
RUN python -m pytest -q

# --- Runtime stage ------------------------------------------------------------
FROM python:3.12-slim AS runtime
WORKDIR /app

ENV PIP_DISABLE_PIP_VERSION_CHECK=1 \
    PIP_NO_CACHE_DIR=1 \
    PYTHONDONTWRITEBYTECODE=1 \
    PYTHONUNBUFFERED=1

# Only runtime deps (no pytest/httpx) — keep image lean
COPY requirements.txt ./
RUN pip install \
        fastapi==0.115.0 \
        "uvicorn[standard]==0.30.6" \
        pydantic==2.9.2

COPY --from=build /src/app ./app

EXPOSE 3000
CMD ["uvicorn", "app.main:app", "--host", "0.0.0.0", "--port", "3000"]
