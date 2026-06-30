import os
import json
import time
import requests
from bs4 import BeautifulSoup
from PIL import Image
import numpy as np

import sys

# Configuration
BASE_URL = "https://artvee.com/c/landscape/page/{}/?filter_orientation=landscape&query_type_orientation=or&per_page=70"
MAX_IMAGES = int(sys.argv[1]) if len(sys.argv) > 1 else 5
OUTPUT_DIR = "Game/Paintings"
JSON_OUTPUT = "Game/Paintings/paintings.json"

# Thresholds for image acceptance
MIN_ASPECT_RATIO = 1.5
MAX_ASPECT_RATIO = 2.0
MIN_WIDTH = 1200
MIN_COLOR_STD = 35.0  # Threshold for color variance (to avoid monochromatic images)

def ensure_dir(path):
    if not os.path.exists(path):
        os.makedirs(path)

def get_detail_links(page_num):
    url = BASE_URL.format(page_num) if page_num > 1 else BASE_URL.format(1).replace("page/1/", "")
    print(f"Fetching list page: {url}")
    headers = {'User-Agent': 'Mozilla/5.0 (Windows NT 10.0; Win64; x64)'}
    response = requests.get(url, headers=headers)
    soup = BeautifulSoup(response.text, 'html.parser')
    
    links = []
    # Find all links that go to the download page
    for a in soup.find_all('a', href=True):
        href = a['href']
        if 'artvee.com/dl/' in href and href not in links:
            links.append(href)
    return links

def parse_detail_page(url):
    print(f"Parsing detail page: {url}")
    headers = {'User-Agent': 'Mozilla/5.0 (Windows NT 10.0; Win64; x64)'}
    response = requests.get(url, headers=headers)
    soup = BeautifulSoup(response.text, 'html.parser')
    
    # Try to find standard download link
    download_url = None
    for a in soup.find_all('a', href=True):
        if 'Standard' in a.text or 'standard' in a.text.lower() or 'max' in a.text.lower():
            if 'artvee.com/dl/?' in a['href'] or 'download' in a['href'].lower():
                 download_url = a['href']
                 break
                 
    # If not found via text, just try finding the direct download button
    if not download_url:
        for a in soup.find_all('a', href=True):
            if 'download' in a.get('class', []) or 'btn' in a.get('class', []):
                if 'dl' in a['href']:
                    download_url = a['href']
                    break

    # Extract metadata
    import re
    title = ""
    artist = ""
    nationality = ""
    date = ""
    artist_years = ""
    
    h1 = soup.find('h1')
    if h1:
        h1_text = h1.text.strip()
        match = re.search(r'\((.*?)\)$', h1_text)
        date = match.group(1).strip() if match else ''
        title = re.sub(r'\s*\((.*?)\)$', '', h1_text).strip()
        
    artist_tag = soup.find('a', href=lambda x: x and '/artist/' in x)
    if artist_tag and artist_tag.parent:
        parent_text = artist_tag.parent.text.strip()
        # Expecting something like "Alexandre Defaux (French, 1826-1900)"
        match = re.match(r'^(.*?)\s*\((.*?),\s*(.*?)\)$', parent_text)
        if match:
            artist = match.group(1).strip()
            nationality = match.group(2).strip()
            artist_years = match.group(3).strip()
        else:
            artist = artist_tag.text.strip()

    final_years = date if date else artist_years

    return {
        'title': title,
        'artist': artist,
        'date': final_years,
        'nationality': nationality,
        'download_url': download_url
    }

def download_image(url, temp_path):
    headers = {'User-Agent': 'Mozilla/5.0'}
    response = requests.get(url, stream=True, headers=headers)
    if response.status_code == 200:
        with open(temp_path, 'wb') as f:
            for chunk in response.iter_content(1024):
                f.write(chunk)
        return True
    return False

def check_image_quality(temp_path):
    try:
        with Image.open(temp_path) as img:
            width, height = img.size
            if width < MIN_WIDTH:
                print(f"Image rejected: width {width} is too small.")
                return False
                
            aspect_ratio = width / height
            if not (MIN_ASPECT_RATIO <= aspect_ratio <= MAX_ASPECT_RATIO):
                print(f"Image rejected: aspect ratio {aspect_ratio:.2f} is not wide enough (needs to be {MIN_ASPECT_RATIO}-{MAX_ASPECT_RATIO}).")
                return False
            
            # Check color variance
            # Convert to RGB, resize for speed, compute standard deviation of channels
            img_small = img.convert('RGB').resize((256, 256))
            arr = np.array(img_small)
            
            # Compute standard deviation across all pixels for each channel
            r_std = np.std(arr[:, :, 0])
            g_std = np.std(arr[:, :, 1])
            b_std = np.std(arr[:, :, 2])
            avg_std = (r_std + g_std + b_std) / 3
            
            if avg_std < MIN_COLOR_STD:
                 print(f"Image rejected: color variance {avg_std:.2f} is too low (monochromatic).")
                 return False
                 
            print(f"Image accepted: {width}x{height}, AR: {aspect_ratio:.2f}, Color Variance: {avg_std:.2f}")
            return True
    except Exception as e:
        print(f"Error checking image: {e}")
        return False

def convert_to_webp(temp_path, final_path):
    try:
        with Image.open(temp_path) as img:
            img.save(final_path, 'WEBP', lossless=True)
        return True
    except Exception as e:
        print(f"Error converting to WebP: {e}")
        return False

def main():
    ensure_dir(OUTPUT_DIR)
    ensure_dir(os.path.dirname(JSON_OUTPUT))
    
    collected = []
    page = 1
    
    while len(collected) < MAX_IMAGES:
        links = get_detail_links(page)
        if not links:
            print("No more links found or blocked.")
            break
            
        for link in links:
            if len(collected) >= MAX_IMAGES:
                break
                
            print(f"--- Processing {link} ---")
            info = parse_detail_page(link)
            if not info['download_url']:
                print("Could not find download URL.")
                continue
                
            # Usually artvee download links are standard HTTP hrefs to images or download scripts
            # We'll try to download it
            temp_path = os.path.join(OUTPUT_DIR, "temp_image.jpg")
            print(f"Downloading from {info['download_url']}...")
            if download_image(info['download_url'], temp_path):
                if check_image_quality(temp_path):
                    # It's a keeper! Convert it
                    import urllib.parse
                    parsed_url = urllib.parse.urlparse(info['download_url'])
                    original_filename = os.path.basename(parsed_url.path)
                    filename = os.path.splitext(original_filename)[0] + ".webp"
                    
                    final_path = os.path.join(OUTPUT_DIR, filename)
                    if convert_to_webp(temp_path, final_path):
                        print(f"Successfully saved {final_path}")
                        collected.append({
                            "file": filename,
                            "name": info['title'],
                            "author": info['artist'],
                            "metadata": {
                                "years": info.get('date', ''),
                                "nationality": info.get('nationality', '')
                            }
                        })
            
            if os.path.exists(temp_path):
                os.remove(temp_path)
                
            time.sleep(1) # Be nice to the server
            
        page += 1

    # Save JSON
    with open(JSON_OUTPUT, 'w', encoding='utf-8') as f:
        json.dump({"items": collected}, f, indent=2, ensure_ascii=False)
        
    print(f"Finished! Collected {len(collected)} images. JSON saved to {JSON_OUTPUT}.")

if __name__ == "__main__":
    main()
