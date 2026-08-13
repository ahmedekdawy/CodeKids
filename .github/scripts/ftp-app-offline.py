"""Put or remove IIS app_offline.htm over FTP so locked DLLs can be replaced."""
from __future__ import annotations

import io
import os
import sys
import time
from ftplib import FTP, FTP_TLS, error_perm


def connect(server: str, user: str, password: str) -> FTP:
    last_error: Exception | None = None
    for factory in (FTP_TLS, FTP):
        try:
            ftp = factory(timeout=90)
            ftp.connect(server, 21)
            ftp.login(user, password)
            if isinstance(ftp, FTP_TLS):
                ftp.prot_p()
            ftp.set_pasv(True)
            return ftp
        except Exception as ex:
            last_error = ex
    raise RuntimeError(f"FTP login failed: {last_error}") from last_error


def cwd_server_dir(ftp: FTP, remote_dir: str) -> None:
    path = (remote_dir or "/").strip() or "/"
    if path in (".", "./"):
        path = "/"
    if not path.startswith("/"):
        path = "/" + path
    ftp.cwd(path)


def put_offline(ftp: FTP) -> None:
    html = b"""<!DOCTYPE html>
<html><head><meta charset="utf-8"><title>Updating</title></head>
<body><p>The site is updating. Please try again in a minute.</p></body></html>
"""
    ftp.storbinary("STOR app_offline.htm", io.BytesIO(html))


def remove_offline(ftp: FTP) -> None:
    try:
        ftp.delete("app_offline.htm")
    except error_perm as ex:
        message = str(ex).lower()
        if "550" not in message and "not found" not in message and "no such" not in message:
            raise


def main() -> int:
    action = sys.argv[1] if len(sys.argv) > 1 else "put"
    server = os.environ["FTP_SERVER"]
    user = os.environ["FTP_USERNAME"]
    password = os.environ["FTP_PASSWORD"]
    remote_dir = os.environ.get("FTP_SERVER_DIR") or "/"

    ftp = connect(server, user, password)
    try:
        cwd_server_dir(ftp, remote_dir)
        if action == "put":
            put_offline(ftp)
        elif action == "delete":
            remove_offline(ftp)
        else:
            raise SystemExit(f"Unknown action: {action}")
    finally:
        try:
            ftp.quit()
        except Exception:
            ftp.close()

    if action == "put":
        time.sleep(10)
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
