# テスト画像生成スクリプト（デバッグ用）

from PIL import Image, ImageDraw, ImageFont
import sys

def create_test_image(output_path, text="こんにちは世界\nHello World"):
    """
    テスト用の画像を生成
    """
    # 画像サイズ
    width, height = 800, 400
    
    # 白背景の画像を作成
    img = Image.new('RGB', (width, height), color='white')
    draw = ImageDraw.Draw(img)
    
    # テキストを描画
    try:
        # システムフォント（日本語対応）
        font = ImageFont.truetype("msgothic.ttc", 40)
    except:
        try:
            font = ImageFont.truetype("arial.ttf", 40)
        except:
            font = ImageFont.load_default()
    
    # テキストを中央に配置
    text_bbox = draw.textbbox((0, 0), text, font=font)
    text_width = text_bbox[2] - text_bbox[0]
    text_height = text_bbox[3] - text_bbox[1]
    
    x = (width - text_width) // 2
    y = (height - text_height) // 2
    
    # 黒文字で描画
    draw.text((x, y), text, fill='black', font=font)
    
    # 保存
    img.save(output_path)
    print(f"テスト画像を生成しました: {output_path}")
    print(f"サイズ: {width}x{height}")
    print(f"テキスト: {text}")

if __name__ == '__main__':
    output = sys.argv[1] if len(sys.argv) > 1 else "test_image.png"
    text = sys.argv[2] if len(sys.argv) > 2 else "こんにちは世界\nHello World"
    
    create_test_image(output, text)
