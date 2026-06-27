import requests
from bs4 import BeautifulSoup
import json

url = "https://artvee.com/c/landscape/page/1/?filter_orientation=landscape&query_type_orientation=or&per_page=10"
headers = {
    'User-Agent': 'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/91.0.4472.124 Safari/537.36'
}
response = requests.get(url, headers=headers)
soup = BeautifulSoup(response.content, 'html.parser')

links = []
for a in soup.find_all('a', href=True):
    if '/dl/' in a['href']:
        links.append(a['href'])

print("Found links:", list(set(links)))

if links:
    detail_url = list(set(links))[0]
    print("Fetching detail url:", detail_url)
    res = requests.get(detail_url, headers=headers)
    dsoup = BeautifulSoup(res.content, 'html.parser')
    
    # Try to find the image container or standard download link
    dl_links = dsoup.find_all('a', href=True)
    std_links = [(a.get_text(strip=True), a['href']) for a in dl_links if 'standard' in a.get_text(strip=True).lower() or 'download' in a.get_text(strip=True).lower()]
    print("Possible download links:", std_links)
    
    # Also find title and artist
    title = dsoup.find('h1')
    if title:
        print("Title:", title.get_text(strip=True))
        
    print("All h2/h3:")
    for h in dsoup.find_all(['h2', 'h3']):
        print(h.get_text(strip=True))
        
    # Artist info
    artist_els = dsoup.find_all('div', class_=lambda c: c and 'artist' in c.lower())
    for el in artist_els:
        print("Artist div:", el.get_text(strip=True))
