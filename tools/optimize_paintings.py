import os
import json
import shutil
import random
import math
from collections import defaultdict

try:
    from PIL import Image
except ImportError:
    import subprocess
    import sys
    subprocess.check_call([sys.executable, "-m", "pip", "install", "Pillow"])
    from PIL import Image

def get_image_metrics(img_path):
    try:
        with Image.open(img_path) as img:
            w, h = img.size
            ar_diff = abs((w / h) - (16 / 9))
            
            # Calculate entropy to detect "flat" or single-color images
            # Convert to grayscale to simplify entropy calculation
            gray_img = img.convert('L')
            histogram = gray_img.histogram()
            histogram_length = sum(histogram)
            samples_probability = [float(h) / histogram_length for h in histogram]
            entropy = -sum([p * math.log(p, 2) for p in samples_probability if p != 0])
            
            return ar_diff, entropy
    except Exception:
        return 999.0, 0.0

def optimize_paintings():
    base_dir = os.path.dirname(os.path.dirname(__file__))
    paintings_dir = os.path.join(base_dir, "paitings")
    tmp_dir = os.path.join(paintings_dir, "tmp")
    json_path = os.path.join(paintings_dir, "paintings.json")
    
    if not os.path.exists(json_path):
        print("paintings.json not found!")
        return

    # Create tmp directory
    os.makedirs(tmp_dir, exist_ok=True)
    
    # Backup json
    shutil.copy(json_path, os.path.join(tmp_dir, "paintings_original.json"))

    # Move all images to tmp
    for f in os.listdir(paintings_dir):
        if f.lower().endswith('.jpg') or f.lower().endswith('.png'):
            src = os.path.join(paintings_dir, f)
            dst = os.path.join(tmp_dir, f)
            shutil.move(src, dst)
            
    # Load data
    with open(json_path, 'r', encoding='utf-8') as f:
        data = json.load(f)
        
    paintings = data.get('paintings', [])
    if not paintings:
        print("No paintings found in JSON.")
        return
        
    print("Calculating aspect ratios and color variance (entropy)...")
    valid_paintings = []
    
    # Process metrics
    for p in paintings:
        src_path = os.path.join(tmp_dir, p['file'])
        ar_diff, entropy = get_image_metrics(src_path)
        p['ar_diff'] = ar_diff
        p['entropy'] = entropy
        
        # Filter out images with very low entropy (flat, single color, lacking detail)
        # 6.0 is a reasonable threshold for an 8-bit grayscale image where max is 8.0
        if entropy >= 6.0:
            valid_paintings.append(p)
        else:
            print(f"Skipping {p['file']} (entropy {entropy:.2f} too low - likely single color)")

    print(f"Filtered out {len(paintings) - len(valid_paintings)} flat/single-color images.")

    # Group by artist
    artist_groups = defaultdict(list)
    for p in valid_paintings:
        artist = p.get('artist', 'Unknown Artist')
        artist_groups[artist].append(p)
        
    # Sort the paintings within each artist by how close they are to 16:9
    for artist in artist_groups:
        artist_groups[artist].sort(key=lambda x: x['ar_diff'])
        
    selected_paintings = []
    artists = list(artist_groups.keys())
    random.shuffle(artists)
    
    # Round-robin selection
    target_count = 150
    while len(selected_paintings) < target_count:
        added_in_round = False
        for artist in artists:
            if artist_groups[artist]:
                selected_paintings.append(artist_groups[artist].pop(0))
                added_in_round = True
                if len(selected_paintings) == target_count:
                    break
        if not added_in_round:
            break # No more paintings available
            
    print(f"Selected {len(selected_paintings)} diverse paintings closest to 16:9.")
    
    # Process selected paintings: convert to WebP
    new_paintings_data = []
    
    for i, p in enumerate(selected_paintings):
        old_filename = p['file']
        src_path = os.path.join(tmp_dir, old_filename)
        
        if not os.path.exists(src_path):
            continue
            
        name, _ = os.path.splitext(old_filename)
        new_filename = f"{name}.webp"
        dst_path = os.path.join(paintings_dir, new_filename)
        
        try:
            with Image.open(src_path) as img:
                if img.mode != 'RGB':
                    img = img.convert('RGB')
                img.save(dst_path, 'webp', quality=80, method=4)
                
            new_p = dict(p)
            new_p['file'] = new_filename
            if 'ar_diff' in new_p:
                del new_p['ar_diff']
            if 'entropy' in new_p:
                del new_p['entropy']
            new_paintings_data.append(new_p)
            
            if (i + 1) % 10 == 0:
                print(f"Converted {i + 1}/{len(selected_paintings)}...")
        except Exception as e:
            print(f"Failed to convert {old_filename}: {e}")
            
    # Save the new JSON
    with open(json_path, 'w', encoding='utf-8') as f:
        json.dump({"paintings": new_paintings_data}, f, indent=4, ensure_ascii=False)
        
    print(f"Finished! Processed {len(new_paintings_data)} images.")

if __name__ == "__main__":
    optimize_paintings()
