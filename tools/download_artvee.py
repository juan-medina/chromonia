import os
import json
import requests
import re
from bs4 import BeautifulSoup
from urllib.parse import urlparse
import time

def download_images(pages=1, per_page=10):
    base_url = "https://artvee.com/c/landscape/page/{}/?filter_orientation=landscape&query_type_orientation=or&per_page={}"
    
    headers = {
        'User-Agent': 'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/91.0.4472.124 Safari/537.36'
    }

    # Setup directories
    paitings_dir = os.path.join(os.path.dirname(os.path.dirname(__file__)), "paitings")
    os.makedirs(paitings_dir, exist_ok=True)
    json_path = os.path.join(paitings_dir, "paintings.json")
    
    # Load existing data
    paintings_data = {"paintings": []}
    if os.path.exists(json_path):
        with open(json_path, 'r', encoding='utf-8') as f:
            try:
                paintings_data = json.load(f)
                if isinstance(paintings_data, list):
                    paintings_data = {"paintings": paintings_data}
            except:
                pass

    downloaded_files = {p.get('file') for p in paintings_data.get('paintings', [])}

    for page in range(1, pages + 1):
        print(f"Fetching page {page}...")
        url = base_url.format(page, per_page)
        response = requests.get(url, headers=headers)
        if response.status_code != 200:
            print(f"Failed to fetch page {page}")
            continue
            
        soup = BeautifulSoup(response.content, 'html.parser')
        
        links = []
        for a in soup.find_all('a', href=True):
            if '/dl/' in a['href']:
                links.append(a['href'])
                
        links = list(set(links))
        print(f"Found {len(links)} painting links on page {page}.")
        
        for link in links:
            print(f"Fetching details for {link}...")
            res = requests.get(link, headers=headers)
            if res.status_code != 200:
                print("Failed to fetch detail page.")
                continue
                
            dsoup = BeautifulSoup(res.content, 'html.parser')
            
            # Title
            title_el = dsoup.find('h1')
            raw_title = title_el.get_text(strip=True) if title_el else "Unknown Title"
            
            title = raw_title
            title_years = ""
            # e.g., A fond farewell (1845)
            t_match = re.search(r'^(.*?)\s*\((\d{4}[-\d]*)\)$', raw_title)
            if t_match:
                title = t_match.group(1).strip()
                title_years = t_match.group(2).strip()
            
            # Artist
            raw_artist = "Unknown Artist"
            artist_els = dsoup.find_all('div', class_=lambda c: c and 'artist' in c.lower())
            for el in artist_els:
                text = el.get_text(strip=True)
                if text:
                    raw_artist = text
                    break
                    
            artist = raw_artist
            nationality = ""
            artist_years = ""
            # e.g., Carlo Bossoli (Italian, 1815-1884) or Christian Rohlfs(German, 1849-1938)
            a_match = re.search(r'^(.*?)\((.*?),\s*(.*?)\)$', raw_artist)
            if a_match:
                artist = a_match.group(1).strip()
                nationality = a_match.group(2).strip()
                artist_years = a_match.group(3).strip()
                
            # Prefer title years if any, else artist years
            years = title_years if title_years else artist_years
                    
            # Download link
            dl_links = dsoup.find_all('a', href=True)
            img_url = None
            for a in dl_links:
                text = a.get_text(strip=True).lower()
                if 'standard' in text or 'download' in text:
                    if '.jpg' in a['href'] or '.png' in a['href']:
                        img_url = a['href']
                        break
                        
            if not img_url:
                print(f"Could not find standard download link for {link}")
                continue
                
            parsed_url = urlparse(img_url)
            filename = os.path.basename(parsed_url.path)
            if not filename:
                filename = f"{hash(link)}.jpg"
                
            if filename in downloaded_files:
                print(f"Skipping {filename}, already downloaded.")
                continue
                
            # Download image
            print(f"Downloading image {filename} from {img_url}...")
            img_res = requests.get(img_url, headers=headers)
            if img_res.status_code == 200:
                filepath = os.path.join(paitings_dir, filename)
                with open(filepath, 'wb') as f:
                    f.write(img_res.content)
                    
                # Append data
                paintings_data['paintings'].append({
                    "file": filename,
                    "title": title,
                    "years": years,
                    "artist": artist,
                    "nationality": nationality
                })
                downloaded_files.add(filename)
                
                # Save JSON
                with open(json_path, 'w', encoding='utf-8') as f:
                    json.dump(paintings_data, f, indent=4, ensure_ascii=False)
                    
                print(f"Successfully saved {title}.")
            else:
                print(f"Failed to download image from {img_url}")
                
            time.sleep(1)

if __name__ == "__main__":
    download_images(pages=10, per_page=70)
