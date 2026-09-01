# -*- coding: utf-8 -*-
"""
STORM IMAGES - Neural Image Editing Backend Server
Powered by Qwen-Image-Edit and ScottzillaSystems/qwen-image-edit-plus-nsfw-lora
FastAPI + Diffusers + PyTorch + Telegram Integration
"""

import os
import io
import time
import base64
import json
import logging
from datetime import datetime
from typing import Optional, Dict, Any, List

from fastapi import FastAPI, HTTPException, Body, File, UploadFile, Form
from fastapi.middleware.cors import CORSMiddleware
from pydantic import BaseModel, Field
from PIL import Image

from telegram_service import TelegramService

logging.basicConfig(level=logging.INFO, format="%(asctime)s [%(levelname)s] %(message)s")
logger = logging.getLogger("storm_images_server")

app = FastAPI(
    title="STORM IMAGES API",
    description="Neural image editing server for Qwen-Image-Edit and LoRA adapters",
    version="0.0.1"
)

app.add_middleware(
    CORSMiddleware,
    allow_origins=["*"],
    allow_credentials=True,
    allow_methods=["*"],
    allow_headers=["*"],
)

# Global Model & Device State
class ModelManager:
    def __init__(self):
        self.pipeline = None
        self.device = "cuda"
        self.torch_dtype = "bfloat16"
        self.base_model_name = "Qwen/Qwen-Image-Edit-2511"
        self.lora_name = "ScottzillaSystems/qwen-image-edit-plus-nsfw-lora"
        self.is_loaded = False
        self.is_loading = False
        self.last_error = None
        self.vram_info = {}

    def get_hardware_info(self) -> Dict[str, Any]:
        info = {
            "cuda_available": False,
            "device_name": "CPU",
            "total_vram_gb": 0.0,
            "free_vram_gb": 0.0,
            "used_vram_gb": 0.0
        }
        try:
            import torch
            if torch.cuda.is_available():
                info["cuda_available"] = True
                info["device_name"] = torch.cuda.get_device_name(0)
                total = torch.cuda.get_device_properties(0).total_memory / (1024 ** 3)
                reserved = torch.cuda.memory_reserved(0) / (1024 ** 3)
                info["total_vram_gb"] = round(total, 2)
                info["used_vram_gb"] = round(reserved, 2)
                info["free_vram_gb"] = round(max(0.0, total - reserved), 2)
        except Exception as ex:
            logger.warning(f"Could not read torch CUDA info: {ex}")
        return info

    def load_pipeline(self, base_model: Optional[str] = None, lora_path: Optional[str] = None):
        import torch
        self.is_loading = True
        self.last_error = None

        if base_model:
            self.base_model_name = base_model
        if lora_path:
            self.lora_name = lora_path

        try:
            logger.info(f"Loading Base Pipeline: {self.base_model_name}...")
            from diffusers import DiffusionPipeline, QwenImageEditPipeline
            
            device = "cuda" if torch.cuda.is_available() else "cpu"
            dtype = torch.bfloat16 if device == "cuda" else torch.float32

            try:
                self.pipeline = QwenImageEditPipeline.from_pretrained(
                    self.base_model_name,
                    torch_dtype=dtype,
                    low_cpu_mem_usage=True
                )
            except Exception:
                self.pipeline = DiffusionPipeline.from_pretrained(
                    self.base_model_name,
                    torch_dtype=dtype,
                    low_cpu_mem_usage=True
                )

            if device == "cuda":
                self.pipeline.to("cuda")

            if self.lora_name:
                logger.info(f"Loading LoRA weights: {self.lora_name}...")
                self.pipeline.load_lora_weights(self.lora_name, adapter_name="edit_lora")
            
            self.is_loaded = True
            self.device = device
            logger.info("Pipeline and LoRA loaded successfully!")
        except Exception as ex:
            self.last_error = str(ex)
            self.is_loaded = False
            logger.error(f"Failed to load pipeline: {ex}")
            raise
        finally:
            self.is_loading = False

model_mgr = ModelManager()

# Request/Response Schemas
class EditRequest(BaseModel):
    image_base64: str = Field(..., description="Source image encoded as Base64 PNG/JPEG")
    prompt: str = Field(..., description="Positive prompt describing desired edits")
    negative_prompt: Optional[str] = Field(default="", description="Negative prompt")
    lora_scale: float = Field(default=0.85, ge=0.0, le=2.0, description="LoRA adapter weight")
    steps: int = Field(default=30, ge=1, le=100, description="Inference steps")
    guidance_scale: float = Field(default=7.5, ge=1.0, le=20.0, description="CFG / Guidance Scale")
    seed: Optional[int] = Field(default=-1, description="Random seed (-1 for random)")
    output_dir: Optional[str] = Field(default=None, description="Custom directory to save output image")
    send_to_telegram: bool = Field(default=False, description="Send result to Telegram")
    telegram_bot_token: Optional[str] = Field(default=None)
    telegram_chat_id: Optional[str] = Field(default=None)
    telegram_caption: Optional[str] = Field(default=None)

class TelegramTestRequest(BaseModel):
    bot_token: str
    chat_id: str

class TelegramSendRequest(BaseModel):
    bot_token: str
    chat_id: str
    image_path: str
    caption: Optional[str] = None


@app.get("/")
def root():
    return {
        "app": "STORM IMAGES AI Backend",
        "version": "0.0.1",
        "status": "online"
    }

@app.get("/v1/status")
def get_status():
    hw = model_mgr.get_hardware_info()
    return {
        "status": "ready" if model_mgr.is_loaded else ("loading" if model_mgr.is_loading else "idle"),
        "base_model": model_mgr.base_model_name,
        "lora_name": model_mgr.lora_name,
        "is_loaded": model_mgr.is_loaded,
        "is_loading": model_mgr.is_loading,
        "last_error": model_mgr.last_error,
        "hardware": hw
    }

@app.post("/v1/model/load")
def load_model(base_model: Optional[str] = None, lora_path: Optional[str] = None):
    try:
        model_mgr.load_pipeline(base_model, lora_path)
        return {"success": True, "message": "Model and LoRA loaded successfully"}
    except Exception as ex:
        raise HTTPException(status_code=500, detail=str(ex))

@app.post("/v1/telegram/test")
async def test_telegram(req: TelegramTestRequest):
    res = await TelegramService.test_connection(req.bot_token, req.chat_id)
    if not res.get("success"):
        raise HTTPException(status_code=400, detail=res.get("error"))
    return res

@app.post("/v1/telegram/send")
async def send_to_telegram(req: TelegramSendRequest):
    res = await TelegramService.send_photo(req.bot_token, req.chat_id, req.image_path, req.caption)
    if not res.get("success"):
        raise HTTPException(status_code=400, detail=res.get("error"))
    return res

@app.post("/v1/edit")
async def edit_image(req: EditRequest):
    start_time = time.time()
    
    # 1. Decode Source Image
    try:
        img_data = base64.b64decode(req.image_base64.split(",")[-1])
        source_image = Image.open(io.BytesIO(img_data)).convert("RGB")
    except Exception as ex:
        raise HTTPException(status_code=400, detail=f"Invalid base64 image: {ex}")

    # 2. Determine Output Directory
    save_folder = req.output_dir
    if not save_folder or not os.path.exists(save_folder):
        default_pictures = os.path.join(os.path.expanduser("~"), "Pictures", "STORM_IMAGES")
        os.makedirs(default_pictures, exist_ok=True)
        save_folder = default_pictures

    # 3. Seed Handling
    import random
    if req.seed is None or req.seed < 0:
        seed = random.randint(0, 2147483647)
    else:
        seed = req.seed

    # 4. Generate or Process Image
    output_image = None
    if model_mgr.is_loaded and model_mgr.pipeline is not None:
        import torch
        generator = torch.Generator(device=model_mgr.device).manual_seed(seed)
        
        try:
            # Set adapter weight
            if hasattr(model_mgr.pipeline, "set_adapters"):
                model_mgr.pipeline.set_adapters(["edit_lora"], adapter_weights=[req.lora_scale])
            
            result = model_mgr.pipeline(
                image=source_image,
                prompt=req.prompt,
                negative_prompt=req.negative_prompt,
                num_inference_steps=req.steps,
                guidance_scale=req.guidance_scale,
                generator=generator
            )
            output_image = result.images[0]
        except Exception as ex:
            logger.error(f"Inference execution error: {ex}")
            raise HTTPException(status_code=500, detail=f"Inference error: {ex}")
    else:
        # Development / Direct Pass-through Mock if weights are not yet downloaded
        logger.warning("Pipeline is not loaded in memory; generating processed placeholder...")
        output_image = source_image.copy()

    # 5. Save Output Image to Output Directory
    timestamp = datetime.now().strftime("%Y%m%d_%H%M%S")
    filename = f"STORM_IMG_{timestamp}_{seed}.png"
    out_path = os.path.join(save_folder, filename)
    output_image.save(out_path, format="PNG")

    # Save metadata JSON
    meta_path = os.path.join(save_folder, f"STORM_IMG_{timestamp}_{seed}.json")
    metadata = {
        "timestamp": timestamp,
        "seed": seed,
        "prompt": req.prompt,
        "negative_prompt": req.negative_prompt,
        "lora_scale": req.lora_scale,
        "steps": req.steps,
        "guidance_scale": req.guidance_scale,
        "base_model": model_mgr.base_model_name,
        "lora_name": model_mgr.lora_name,
        "image_file": filename,
        "width": output_image.width,
        "height": output_image.height
    }
    try:
        with open(meta_path, "w", encoding="utf-8") as f:
            json.dump(metadata, f, indent=2, ensure_ascii=False)
    except Exception as ex:
        logger.warning(f"Could not write metadata JSON: {ex}")

    # 6. Telegram Dispatch
    telegram_status = {"dispatched": False}
    if req.send_to_telegram and req.telegram_bot_token and req.telegram_chat_id:
        caption = req.telegram_caption or f"⚡ *STORM IMAGES 0.0.1*\n🎨 *Prompt*: {req.prompt}\n🎲 *Seed*: `{seed}`\n✨ *LoRA Scale*: `{req.lora_scale}`"
        tg_res = await TelegramService.send_photo(
            bot_token=req.telegram_bot_token,
            chat_id=req.telegram_chat_id,
            image_path=out_path,
            caption=caption
        )
        telegram_status = {
            "dispatched": tg_res.get("success", False),
            "error": tg_res.get("error"),
            "message_id": tg_res.get("message_id")
        }

    # 7. Convert Result to Base64
    buf = io.BytesIO()
    output_image.save(buf, format="PNG")
    res_b64 = "data:image/png;base64," + base64.b64encode(buf.getvalue()).decode("utf-8")

    elapsed = round(time.time() - start_time, 2)
    return {
        "success": True,
        "image_base64": res_b64,
        "file_path": out_path,
        "filename": filename,
        "seed": seed,
        "width": output_image.width,
        "height": output_image.height,
        "generation_time_seconds": elapsed,
        "telegram": telegram_status,
        "metadata": metadata
    }

if __name__ == "__main__":
    import uvicorn
    uvicorn.run(app, host="127.0.0.1", port=7860)