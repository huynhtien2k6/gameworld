"""Smoke-test the production image with a disposable volume and random credentials."""
import json
import os
import secrets
import subprocess
import time
import urllib.error
import urllib.request
import uuid

name = "gameworld-verify-" + uuid.uuid4().hex[:12]
volume = name + "-data"
base = "http://127.0.0.1:18080"
env = dict(os.environ, JwtSettings__Key=secrets.token_hex(64),
           BootstrapAdmin__Username="verify_admin", BootstrapAdmin__Password=secrets.token_hex(24))


def docker(*args):
    return subprocess.check_output(["docker", *args], env=env, text=True).strip()


def request(path, data=None, token=None, expected=200, forwarded=True):
    headers = {"Content-Type": "application/json"}
    if forwarded:
        headers["X-Forwarded-Proto"] = "https"
    if token:
        headers["Authorization"] = "Bearer " + token
    req = urllib.request.Request(base + path,
        data=None if data is None else json.dumps(data).encode(), headers=headers)
    try:
        response = urllib.request.urlopen(req, timeout=5)
    except urllib.error.HTTPError as error:
        response = error
    with response:
        assert response.status == expected, f"{path}: expected {expected}, received {response.status}"
        body = response.read()
        return json.loads(body) if response.headers.get_content_type() == "application/json" else body


def ready():
    for _ in range(80):
        try:
            assert request("/health")["status"] == "healthy"
            return
        except (OSError, AssertionError):
            time.sleep(0.25)
    raise AssertionError("Production container did not become healthy")


try:
    docker("volume", "create", volume)
    docker("run", "-d", "--name", name, "--network", "host",
           "-e", "JwtSettings__Key", "-e", "BootstrapAdmin__Username", "-e", "BootstrapAdmin__Password",
           "-e", "ASPNETCORE_URLS=http://127.0.0.1:18080", "-e", "ASPNETCORE_HTTPS_PORT=443",
           "-e", "Database__Provider=Sqlite", "-e", "ConnectionStrings__DefaultConnection=Data Source=/app/App_Data/gameworld.db",
           "-v", volume + ":/app/App_Data", "gameworld:ci")
    ready()
    assert docker("exec", name, "id", "-u") != "0", "API must run as a non-root user"
    for path in ("/", "/admin.html", "/play.html"):
        assert request(path), f"Missing static page: {path}"
    request("/api/admin/overview", expected=401)
    password = secrets.token_hex(24)
    request("/api/auth/register", {"username": "container_player", "email": "container@example.test", "password": password})
    docker("restart", name)
    ready()
    session = request("/api/auth/login", {"username": "container_player", "password": password})
    request("/api/auth/me", token=session["token"])
    admin = request("/api/auth/admin/login", {"username": env["BootstrapAdmin__Username"], "password": env["BootstrapAdmin__Password"]})
    assert request("/api/admin/overview", token=admin["token"])["accounts"] == 1
    print("PASS: non-root production image, HTTPS proxy headers, static pages, authorization, persistent SQLite and restart")
finally:
    subprocess.run(["docker", "rm", "-f", name], stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL)
    subprocess.run(["docker", "volume", "rm", volume], stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL)
