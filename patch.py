import re

def update_scene(filepath, is_settings=False):
    with open(filepath, 'r', encoding='utf-8') as f:
        content = f.read()
    
    # 1. Add ext_resource definitions if not present
    resources = {
        'play.svg': 'icon_play',
        'options.svg': 'icon_options',
        'about.svg': 'icon_about',
        'exit.svg': 'icon_exit',
        'back.svg': 'icon_back',
        'restart.svg': 'icon_restart',
        'home.svg': 'icon_home',
        'difficulty.svg': 'icon_diff',
        'volume_master.svg': 'icon_vol_m',
        'volume_music.svg': 'icon_vol_mu',
        'volume_sfx.svg': 'icon_vol_sfx',
        'fullscreen.svg': 'icon_fs'
    }
    
    ext_blocks = []
    for filename, res_id in resources.items():
        if f'id="{res_id}"' not in content:
            ext_blocks.append(f'[ext_resource type="Texture2D" path="res://Assets/Icons/{filename}" id="{res_id}"]\n')
            
    if ext_blocks:
        # insert before first node or sub_resource
        match = re.search(r'\n\[(?:sub_resource|node)', content)
        if match:
            idx = match.start()
            content = content[:idx] + '\n' + ''.join(ext_blocks) + content[idx:]

    # 2. Replacements for Main and Pause menu
    reps = [
        (r'text = "? Play"', 'text = "Play"\nicon = ExtResource("icon_play")'),
        (r'text = "? Resume"', 'text = "Resume"\nicon = ExtResource("icon_play")'),
        (r'text = "? Options"', 'text = "Options"\nicon = ExtResource("icon_options")'),
        (r'text = "? About"', 'text = "About"\nicon = ExtResource("icon_about")'),
        (r'text = "? Exit"', 'text = "Exit"\nicon = ExtResource("icon_exit")'),
        (r'text = "? Back"', 'text = "Back"\nicon = ExtResource("icon_back")'),
        (r'text = "? Restart"', 'text = "Restart"\nicon = ExtResource("icon_restart")'),
        (r'text = "? Main Menu"', 'text = "Main Menu"\nicon = ExtResource("icon_home")'),
        
        # Difficulty menu (which had no unicode icons before)
        (r'text = "Normal"', 'text = "Normal"\nicon = ExtResource("icon_diff")'),
        (r'text = "Medium"', 'text = "Medium"\nicon = ExtResource("icon_diff")'),
        (r'text = "Hard"', 'text = "Hard"\nicon = ExtResource("icon_diff")'),
        (r'text = "Zen"', 'text = "Zen"\nicon = ExtResource("icon_diff")'),
    ]
    
    if not is_settings:
        for old, new in reps:
            content = content.replace(old, new)

    if is_settings:
        # For settings, we need to convert Labels to Buttons
        # "SettingsLabel"
        content = re.sub(
            r'\[node name="SettingsLabel" type="Label"([^\]]*)\]\nlayout_mode = 2\ntheme_override_colors/font_color = Color\(1, 0, 1, 1\)\ntheme_override_font_sizes/font_size = 32\ntext = "? Settings"',
            r'[node name="SettingsLabel" type="Button"\1]\nlayout_mode = 2\ntheme_override_colors/font_color = Color(1, 0, 1, 1)\ntheme_override_font_sizes/font_size = 32\ntext = "Settings"\nicon = ExtResource("icon_options")\nflat = true\nfocus_mode = 0\nmouse_filter = 2',
            content
        )
        # MasterLabel
        content = re.sub(
            r'\[node name="MasterLabel" type="Label"([^\]]*)\]\nlayout_mode = 2\ntheme_override_font_sizes/font_size = 18\ntext = "Master Volume"',
            r'[node name="MasterLabel" type="Button"\1]\nlayout_mode = 2\ntheme_override_font_sizes/font_size = 18\ntext = "Master Volume"\nicon = ExtResource("icon_vol_m")\nflat = true\nfocus_mode = 0\nmouse_filter = 2\nalignment = 0',
            content
        )
        # MusicLabel
        content = re.sub(
            r'\[node name="MusicLabel" type="Label"([^\]]*)\]\nlayout_mode = 2\ntheme_override_font_sizes/font_size = 18\ntext = "Music Volume"',
            r'[node name="MusicLabel" type="Button"\1]\nlayout_mode = 2\ntheme_override_font_sizes/font_size = 18\ntext = "Music Volume"\nicon = ExtResource("icon_vol_mu")\nflat = true\nfocus_mode = 0\nmouse_filter = 2\nalignment = 0',
            content
        )
        # SfxLabel
        content = re.sub(
            r'\[node name="SfxLabel" type="Label"([^\]]*)\]\nlayout_mode = 2\ntheme_override_font_sizes/font_size = 18\ntext = "SFX Volume"',
            r'[node name="SfxLabel" type="Button"\1]\nlayout_mode = 2\ntheme_override_font_sizes/font_size = 18\ntext = "SFX Volume"\nicon = ExtResource("icon_vol_sfx")\nflat = true\nfocus_mode = 0\nmouse_filter = 2\nalignment = 0',
            content
        )

    with open(filepath, 'w', encoding='utf-8') as f:
        f.write(content)

update_scene('Game/MainMenu/main_menu.tscn')
update_scene('Game/PauseMenu/pause_menu.tscn')
update_scene('Game/Settings/settings_panel.tscn', is_settings=True)
print("Updated all scenes.")
