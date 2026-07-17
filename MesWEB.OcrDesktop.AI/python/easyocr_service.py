"""
EasyOCR サービス - .NET から呼び出される Python スクリプト
"""
import sys
import json
import os

# UTF-8 エンコーディングを強制設定
if sys.stdout.encoding != 'utf-8':
    sys.stdout.reconfigure(encoding='utf-8')
if sys.stderr.encoding != 'utf-8':
    sys.stderr.reconfigure(encoding='utf-8')

# 環境変数でも UTF-8 を設定
os.environ['PYTHONIOENCODING'] = 'utf-8'

import easyocr

def main():
    if len(sys.argv) < 4:
        print("使用方法: python easyocr_service.py <image_path> <result_path> <language>", file=sys.stderr)
        sys.exit(1)
    
    image_path = sys.argv[1]
    result_path = sys.argv[2]
    language = sys.argv[3]
    
    try:
        print(f"[DEBUG] 画像パス: {image_path}", file=sys.stderr)
        print(f"[DEBUG] 結果パス: {result_path}", file=sys.stderr)
        print(f"[DEBUG] 言語: {language}", file=sys.stderr)
        
        # ファイル存在確認
        if not os.path.exists(image_path):
            raise FileNotFoundError(f"画像ファイルが見つかりません: {image_path}")
        
        file_size = os.path.getsize(image_path)
        print(f"[DEBUG] 画像ファイルサイズ: {file_size} bytes", file=sys.stderr)
        
        # EasyOCR リーダーを初期化
        print(f"[DEBUG] EasyOCR リーダーを初期化中...", file=sys.stderr)
        languages = ['ja', 'en'] if language == 'ja' else ['en']
        
        # verbose=False で進捗バーを無効化（エンコーディングエラー回避）
        reader = easyocr.Reader(languages, gpu=False, verbose=False)
        print(f"[DEBUG] EasyOCR リーダー初期化完了", file=sys.stderr)
        
        # OCR を実行
        print(f"[DEBUG] OCR 実行中...", file=sys.stderr)
        results = reader.readtext(image_path, paragraph=False)
        print(f"[DEBUG] OCR 完了。結果件数: {len(results)}", file=sys.stderr)
        
        # テキストを抽出
        text_lines = []
        for i, (bbox, text, prob) in enumerate(results):
            print(f"[DEBUG] 結果 {i+1}: テキスト='{text}', 信頼度={prob:.2f}", file=sys.stderr)
            text_lines.append(text)
        
        extracted_text = '\n'.join(text_lines)
        print(f"[DEBUG] 抽出テキスト ({len(extracted_text)} 文字): {extracted_text[:100]}", file=sys.stderr)
        
        # 結果を JSON ファイルに保存
        result = {
            'text': extracted_text,
            'success': True,
            'details': {
                'total_blocks': len(results),
                'total_chars': len(extracted_text)
            }
        }
        
        print(f"[DEBUG] 結果を保存中: {result_path}", file=sys.stderr)
        with open(result_path, 'w', encoding='utf-8') as f:
            json.dump(result, f, ensure_ascii=False, indent=2)
        
        print(f"OCR 完了: {len(text_lines)} 行のテキストを検出")
        
    except Exception as e:
        error_msg = str(e)
        print(f"[ERROR] {error_msg}", file=sys.stderr)
        
        import traceback
        traceback.print_exc(file=sys.stderr)
        
        # エラー情報を JSON ファイルに保存
        error_result = {
            'text': '',
            'success': False,
            'error': error_msg
        }
        
        with open(result_path, 'w', encoding='utf-8') as f:
            json.dump(error_result, f, ensure_ascii=False, indent=2)
        
        sys.exit(1)

if __name__ == '__main__':
    main()
