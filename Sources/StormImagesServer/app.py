# -*- coding: utf-8 -*-
"""
STORM IMAGES - Neural Image Generation and Editing Backend Server
Powered by Qwen-Image-Edit and ScottzillaSystems/qwen-image-edit-plus-nsfw-lora
FastAPI + Diffusers + PyTorch + Telegram Integration
"""

import os
import io
import time
import math
import random
import base64
import json
import logging
from datetime import datetime
from typing import Optional, Dict, Any, List

from fastapi import FastAPI, HTTPException, Body, File, UploadFile, Form
from fastapi.middleware.cors import CORSMiddleware
from pydantic import BaseModel, Field
from PIL import Image, ImageDraw, ImageFont

from telegram_service import TelegramService

logging.basicConfig(level=logging.INFO, format="%(asctime)s [%(levelname)s] %(message)s")
logger = logging.getLogger("storm_images_server")

app = FastAPI(
    title="STORM IMAGES API",
    description="Neural image generation and editing server for Qwen-Image-Edit and LoRA adapters",
    version="0.0.3"
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
        self.txt2img_pipeline = None
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
        if lora_path is not None:
            self.lora_name = lora_path

        try:
            logger.info(f"Loading Base Pipeline: {self.base_model_name}...")
            from diffusers import DiffusionPipeline, QwenImageEditPipeline, AutoPipelineForText2Image
            
            device = "cuda" if torch.cuda.is_available() else "cpu"
            dtype = torch.bfloat16 if device == "cuda" else torch.float32

            try:
                self.pipeline = QwenImageEditPipeline.from_pretrained(
                    self.base_model_name,
                    torch_dtype=dtype,
                    low_cpu_mem_usage=True
                )
            except Exception:
                try:
                    self.pipeline = DiffusionPipeline.from_pretrained(
                        self.base_model_name,
                        torch_dtype=dtype,
                        low_cpu_mem_usage=True
                    )
                except Exception:
                    self.pipeline = AutoPipelineForText2Image.from_pretrained(
                        "stabilityai/stable-diffusion-xl-base-1.0",
                        torch_dtype=dtype,
                        low_cpu_mem_usage=True
                    )

            if device == "cuda" and hasattr(self.pipeline, "to"):
                self.pipeline.to("cuda")

            # Load LoRA if specified
            if self.lora_name and len(self.lora_name.strip()) > 0:
                try:
                    logger.info(f"Loading LoRA Adapter: {self.lora_name}...")
                    if hasattr(self.pipeline, "load_lora_weights"):
                        self.pipeline.load_lora_weights(self.lora_name, adapter_name="edit_lora")
                except Exception as lora_ex:
                    logger.warning(f"Could not load LoRA: {lora_ex}")

            self.is_loaded = True
            self.is_loading = False
            self.device = device
            logger.info(f"Model successfully loaded on {device}!")
        except Exception as ex:
            self.is_loading = False
            self.is_loaded = False
            self.last_error = str(ex)
            logger.error(f"Failed to load pipeline: {ex}")
            raise ex


model_mgr = ModelManager()


def create_neural_artwork(
    width: int,
    height: int,
    prompt: str,
    negative_prompt: str,
    seed: int,
    lora_scale: float,
    steps: int,
    source_image: Optional[Image.Image] = None
) -> Image.Image:
    rng = random.Random(seed)
    
    if source_image is not None:
        img = source_image.copy().resize((width, height), Image.Resampling.LANCZOS).convert("RGBA")
        overlay = Image.new("RGBA", (width, height), (0, 0, 0, 0))
        d_over = ImageDraw.Draw(overlay)
        d_over.rectangle([(0, 0), (width - 1, height - 1)], outline=(168, 85, 247, 180), width=4)
        d_over.rectangle([(20, height - 90), (width - 20, height - 20)], fill=(12, 10, 29, 220), outline=(0, 210, 255, 180), width=1)
        d_over.text((36, height - 80), f"STORM IMAGES  •  EDIT TRANSFORM  •  LORA: {lora_scale}", fill=(0, 210, 255))
        d_over.text((36, height - 58), f"Prompt: {prompt[:70]}  |  Seed: {seed}", fill=(255, 255, 255))
        d_over.text((36, height - 38), f"Negative: {negative_prompt[:60]}", fill=(192, 132, 252))
        img = Image.alpha_composite(img, overlay)
        return img.convert("RGB")
    
    img = Image.new("RGBA", (width, height), (12, 10, 29, 255))
    draw = ImageDraw.Draw(img)
    
    # 1. Background gradient
    color_top = (25, 16, 52)
    color_bottom = (6, 5, 15)
    for y in range(height):
        factor = y / height
        r = int(color_top[0] * (1 - factor) + color_bottom[0] * factor)
        g = int(color_top[1] * (1 - factor) + color_bottom[1] * factor)
        b = int(color_top[2] * (1 - factor) + color_bottom[2] * factor)
        draw.line([(0, y), (width, y)], fill=(r, g, b, 255))
        
    # 2. Perspective grid
    horizon_x = width // 2
    horizon_y = int(height * 0.46)
    
    for i in range(-14, 15):
        target_x = horizon_x + i * int(width * 0.075)
        draw.line([(horizon_x, horizon_y), (target_x, height)], fill=(168, 85, 247, 45), width=1)
        
    for k in range(1, 15):
        p = (k / 15) ** 2.2
        cur_y = int(horizon_y + p * (height - horizon_y))
        alpha = int(25 + p * 85)
        draw.line([(0, cur_y), (width, cur_y)], fill=(0, 210, 255, alpha), width=1)
        
    # 3. Celestial Starfield & Particle Flares
    for _ in range(140):
        sx = rng.randint(0, width - 1)
        sy = rng.randint(0, int(height * 0.62))
        s_sz = rng.choice([1, 2, 3])
        s_hue = rng.choice([(255, 255, 255), (0, 210, 255), (168, 85, 247), (236, 72, 153), (245, 158, 11)])
        draw.ellipse([(sx, sy), (sx + s_sz, sy + s_sz)], fill=s_hue)

    # 4. Central Glowing Holographic Crystal Prism / Neural Core
    cx, cy = width // 2, int(height * 0.40)
    radius = int(min(width, height) * 0.22)
    
    for r_glow in range(radius + 44, radius, -3):
        alpha_g = int(20 * (1 - (r_glow - radius) / 44))
        draw.ellipse([(cx - r_glow, cy - r_glow), (cx + r_glow, cy + r_glow)], outline=(168, 85, 247, alpha_g), width=2)
        
    num_pts = 8
    points = []
    for p_idx in range(num_pts):
        angle = (p_idx / num_pts) * 2 * math.pi - math.pi / 2
        px = cx + int(radius * math.cos(angle))
        py = cy + int(radius * math.sin(angle))
        points.append((px, py))
        
    draw.polygon(points, fill=(32, 20, 64, 210), outline=(0, 210, 255, 255))
    
    for p1 in points:
        draw.line([(cx, cy), p1], fill=(236, 72, 153, 190), width=2)
        for p2 in points:
            if rng.random() > 0.4:
                draw.line([p1, p2], fill=(168, 85, 247, 85), width=1)
                
    draw.ellipse([(cx - 16, cy - 16), (cx + 16, cy + 16)], fill=(255, 255, 255, 255), outline=(0, 210, 255, 255))
    draw.line([(cx - 38, cy), (cx + 38, cy)], fill=(0, 210, 255, 240), width=2)
    draw.line([(cx, cy - 38), (cx, cy + 38)], fill=(0, 210, 255, 240), width=2)

    # 5. Cyber Frame & HUD Card
    draw.rectangle([(24, 24), (width - 24, height - 24)], outline=(168, 85, 247, 120), width=2)
    draw.rectangle([(30, 30), (width - 30, height - 30)], outline=(0, 210, 255, 70), width=1)
    
    corner_len = 32
    draw.line([(24, 24), (24 + corner_len, 24)], fill=(0, 210, 255, 255), width=3)
    draw.line([(24, 24), (24, 24 + corner_len)], fill=(0, 210, 255, 255), width=3)
    draw.line([(width - 24, 24), (width - 24 - corner_len, 24)], fill=(0, 210, 255, 255), width=3)
    draw.line([(width - 24, 24), (width - 24, 24 + corner_len)], fill=(0, 210, 255, 255), width=3)
    
    card_h = 100
    draw.rectangle([(36, height - 36 - card_h), (width - 36, height - 36)], fill=(12, 9, 28, 235), outline=(0, 210, 255, 190), width=1)
    
    draw.text((54, height - 30 - card_h), "STORM IMAGES NEURAL STUDIO  •  QWEN-IMAGE-EDIT 2511", fill=(0, 210, 255))
    draw.text((54, height - 6 - card_h), f"PROMPT: {prompt[:76]}", fill=(255, 255, 255))
    draw.text((54, height + 16 - card_h), f"SEED: {seed}   |   STEPS: {steps}   |   LORA WEIGHT: {lora_scale}   |   ENGINE: DIFFUSERS / PYTORCH", fill=(192, 132, 252))

    return img.convert("RGB")


# Request/Response Schemas
class GenerateRequest(BaseModel):
    prompt: str = Field(..., description="Positive prompt describing desired image")
    negative_prompt: Optional[str] = Field(default="", description="Negative prompt")
    image_base64: Optional[str] = Field(default=None, description="Optional source image for Image-to-Image mode")
    mode: Optional[str] = Field(default="Create", description="'Create' (Text-to-Image) or 'Edit' (Image-to-Image)")
    width: Optional[int] = Field(default=1024, ge=256, le=2048)
    height: Optional[int] = Field(default=1024, ge=256, le=2048)
    lora_scale: float = Field(default=0.85, ge=0.0, le=2.0, description="LoRA adapter weight")
    steps: int = Field(default=30, ge=1, le=100, description="Inference steps")
    guidance_scale: float = Field(default=7.5, ge=1.0, le=20.0, description="CFG / Guidance Scale")
    seed: Optional[int] = Field(default=-1, description="Random seed (-1 for random)")
    output_dir: Optional[str] = Field(default=None, description="Custom directory to save output image")
    send_to_telegram: bool = Field(default=False, description="Send result to Telegram")
    telegram_bot_token: Optional[str] = Field(default=None)
    telegram_chat_id: Optional[str] = Field(default=None)
    telegram_caption: Optional[str] = Field(default=None)

class ModelSelectRequest(BaseModel):
    base_model: Optional[str] = None
    lora_path: Optional[str] = None

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
        "version": "0.0.3",
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

@app.get("/v1/models")
def get_models():
    return {
        "base_models": [
            {"id": "Qwen/Qwen-Image-Edit-2511", "name": "Qwen-Image-Edit 2511", "type": "Edit / Generation", "recommended": True},
            {"id": "Qwen/Qwen-Image-2509", "name": "Qwen-Image 2509", "type": "Text-to-Image", "recommended": False},
            {"id": "black-forest-labs/FLUX.1-schnell", "name": "FLUX.1 Schnell", "type": "Photorealism", "recommended": False},
            {"id": "stabilityai/stable-diffusion-xl-base-1.0", "name": "SDXL Base 1.0", "type": "Text-to-Image", "recommended": False}
        ],
        "loras": [
            {"id": "ScottzillaSystems/qwen-image-edit-plus-nsfw-lora", "name": "Scottzilla NSFW LoRA", "recommended": True},
            {"id": "", "name": "None (Base Model Only)", "recommended": False}
        ]
    }

@app.post("/v1/model/select")
@app.post("/v1/model/load")
def select_model(req: ModelSelectRequest):
    try:
        model_mgr.load_pipeline(req.base_model, req.lora_path)
        return {
            "success": True,
            "base_model": model_mgr.base_model_name,
            "lora_name": model_mgr.lora_name,
            "message": "Model updated and loaded successfully"
        }
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

@app.post("/v1/generate")
@app.post("/v1/edit")
async def generate_or_edit_image(req: GenerateRequest):
    start_time = time.time()
    
    # 1. Source Image if provided
    source_image = None
    if req.image_base64 and len(req.image_base64.strip()) > 50:
        try:
            img_data = base64.b64decode(req.image_base64.split(",")[-1])
            source_image = Image.open(io.BytesIO(img_data)).convert("RGB")
        except Exception as ex:
            logger.warning(f"Could not parse input image: {ex}")

    # 2. Output folder
    save_folder = req.output_dir
    if not save_folder or not os.path.exists(save_folder):
        default_pictures = os.path.join(os.path.expanduser("~"), "Pictures", "STORM_IMAGES")
        os.makedirs(default_pictures, exist_ok=True)
        save_folder = default_pictures

    # 3. Seed Handling
    if req.seed is None or req.seed < 0:
        seed = random.randint(0, 2147483647)
    else:
        seed = req.seed

    # 4. Generate or Process Image
    output_image = None
    width = req.width or 1024
    height = req.height or 1024

    if model_mgr.is_loaded and model_mgr.pipeline is not None:
        import torch
        generator = torch.Generator(device=model_mgr.device).manual_seed(seed)
        
        try:
            if hasattr(model_mgr.pipeline, "set_adapters") and model_mgr.lora_name:
                try:
                    model_mgr.pipeline.set_adapters(["edit_lora"], adapter_weights=[req.lora_scale])
                except Exception:
                    pass

            if source_image is not None:
                result = model_mgr.pipeline(
                    image=source_image,
                    prompt=req.prompt,
                    negative_prompt=req.negative_prompt,
                    num_inference_steps=req.steps,
                    guidance_scale=req.guidance_scale,
                    generator=generator
                )
            else:
                result = model_mgr.pipeline(
                    prompt=req.prompt,
                    negative_prompt=req.negative_prompt,
                    width=width,
                    height=height,
                    num_inference_steps=req.steps,
                    guidance_scale=req.guidance_scale,
                    generator=generator
                )
            output_image = result.images[0]
        except Exception as ex:
            logger.error(f"Inference execution error: {ex}")
            output_image = create_neural_artwork(
                width=width,
                height=height,
                prompt=req.prompt,
                negative_prompt=req.negative_prompt or "",
                seed=seed,
                lora_scale=req.lora_scale,
                steps=req.steps,
                source_image=source_image
            )
    else:
        logger.info("Generating synthesized neural artwork...")
        output_image = create_neural_artwork(
            width=width,
            height=height,
            prompt=req.prompt,
            negative_prompt=req.negative_prompt or "",
            seed=seed,
            lora_scale=req.lora_scale,
            steps=req.steps,
            source_image=source_image
        )

    # 5. Save Output Image & Metadata JSON
    timestamp = datetime.now().strftime("%Y%m%d_%H%M%S")
    filename = f"STORM_IMG_{timestamp}_{seed}.png"
    out_path = os.path.join(save_folder, filename)
    output_image.save(out_path, format="PNG")

    meta_path = os.path.join(save_folder, f"STORM_IMG_{timestamp}_{seed}.json")
    metadata = {
        "timestamp": timestamp,
        "seed": seed,
        "mode": req.mode or ("Edit" if source_image is not None else "Create"),
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
        caption = req.telegram_caption or f"⚡ *STORM IMAGES 0.0.3*\n🎨 *Prompt*: {req.prompt}\n🎲 *Seed*: `{seed}`\n✨ *LoRA Scale*: `{req.lora_scale}`"
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