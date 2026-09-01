# -*- coding: utf-8 -*-
"""
STORM IMAGES - Telegram Bot Integration Service
Provides methods to verify credentials and dispatch generated artwork to Telegram channels/groups.
"""

import os
import logging
import httpx
from typing import Dict, Any, Optional

logger = logging.getLogger("storm_telegram")

class TelegramService:
    @staticmethod
    async def test_connection(bot_token: str, chat_id: str) -> Dict[str, Any]:
        if not bot_token:
            return {"success": False, "error": "Bot token is empty"}
        if not chat_id:
            return {"success": False, "error": "Chat / Channel ID is empty"}

        bot_token = bot_token.strip()
        chat_id = chat_id.strip()

        async with httpx.AsyncClient(timeout=10.0) as client:
            try:
                # 1. Verify Bot Token
                me_resp = await client.get(f"https://api.telegram.org/bot{bot_token}/getMe")
                if me_resp.status_code != 200:
                    return {"success": False, "error": f"Invalid Bot Token: {me_resp.text}"}
                
                bot_info = me_resp.json().get("result", {})
                bot_username = bot_info.get("username", "UnknownBot")

                # 2. Verify Chat Access
                chat_resp = await client.get(
                    f"https://api.telegram.org/bot{bot_token}/getChat",
                    params={"chat_id": chat_id}
                )
                if chat_resp.status_code != 200:
                    return {
                        "success": False,
                        "error": f"Cannot access chat/channel {chat_id}. Ensure bot @{bot_username} is added as Admin: {chat_resp.text}"
                    }
                
                chat_info = chat_resp.json().get("result", {})
                chat_title = chat_info.get("title") or chat_info.get("username") or chat_id

                return {
                    "success": True,
                    "bot_username": bot_username,
                    "chat_title": chat_title,
                    "chat_id": chat_id,
                    "message": f"Connected to @{bot_username} -> '{chat_title}'"
                }
            except Exception as ex:
                logger.error(f"Telegram test connection failed: {ex}")
                return {"success": False, "error": str(ex)}

    @staticmethod
    async def send_photo(
        bot_token: str,
        chat_id: str,
        image_path: str,
        caption: Optional[str] = None
    ) -> Dict[str, Any]:
        if not bot_token or not chat_id:
            return {"success": False, "error": "Telegram credentials missing"}
        if not os.path.exists(image_path):
            return {"success": False, "error": f"Image file not found: {image_path}"}

        bot_token = bot_token.strip()
        chat_id = chat_id.strip()

        async with httpx.AsyncClient(timeout=30.0) as client:
            try:
                with open(image_path, "rb") as img_file:
                    files = {"photo": (os.path.basename(image_path), img_file, "image/png")}
                    data = {"chat_id": chat_id}
                    if caption:
                        data["caption"] = caption[:1024]
                        data["parse_mode"] = "Markdown"

                    resp = await client.post(
                        f"https://api.telegram.org/bot{bot_token}/sendPhoto",
                        data=data,
                        files=files
                    )
                    
                    if resp.status_code == 200:
                        res_json = resp.json()
                        message_id = res_json.get("result", {}).get("message_id")
                        return {"success": True, "message_id": message_id}
                    else:
                        return {"success": False, "error": f"Telegram API error: {resp.text}"}
            except Exception as ex:
                logger.error(f"Failed to send photo to Telegram: {ex}")
                return {"success": False, "error": str(ex)}