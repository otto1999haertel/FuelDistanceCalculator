import json
import os
import re
import camelot
from pathlib import Path
from datetime import datetime, timezone

Input_PDF = "autokostenuebersicht.pdf"
Output_JSON = "ADAC_car_data.json"

current_dir = os.path.dirname(os.path.abspath(__file__))
output_json_dir = os.path.abspath(os.path.join(current_dir, "..", "FuelDistanceCalculator", "Data"))

downloads_path = str(Path.home() / "Downloads")
file_path = os.path.join(downloads_path, Input_PDF)

print(f"Looking for PDF in {downloads_path}")

if not os.path.exists(file_path):
    raise FileNotFoundError(f"File {Input_PDF} not found in {downloads_path}")

if not os.path.exists(output_json_dir):
    raise NotADirectoryError(f"Output dir {output_json_dir} not found")

data = {}
brand = ""

camelot_tables = camelot.read_pdf(file_path, pages="3-end", flavor="stream")
for table in camelot_tables:
    for row in table.data[4:]:
        model = " ".join(row[0].splitlines()).strip()
        power = row[1].strip()
        cost_per_km = row[-1].strip()

        if not power and not cost_per_km:
            if "EUR/h" in model:
                brand = re.sub(r'\s*\d+\s*EUR/h.*', '', model).strip()
            else:
                if data:
                    last_key = list(data.keys())[-1]
                    last_value = data.pop(last_key)
                    parts = last_key.rsplit(' ', 2)  # trennt "kW" und Power-Zahl ab
                    new_key = f"{parts[0]} {model} {parts[1]} {parts[2]}".strip()
                    data[new_key] = last_value
            continue

        if cost_per_km and re.match(r'^\d+[,\.]\d+$', cost_per_km.strip()):
            power_clean = re.search(r'\d+$', power.strip())
            if power_clean:
                power = power_clean.group()
            cost_per_km = str(round(float(cost_per_km.replace(",", ".")) / 100, 4))
            key = f"{brand} {model} {power} kW".strip()
            data[key] = cost_per_km
            print(f"✓ {key}: {data[key]}")

output = {
    "metadata": {
        "source": "ADAC Autokosten Herbst/Winter 2025",
        "generated_at": datetime.now(timezone.utc).isoformat(),
        "entry_count": len(data)
    },
    "cars": data
}

output_path = os.path.join(output_json_dir, Output_JSON)
with open(output_path, "w", encoding="utf-8") as f:
    json.dump(output, f, ensure_ascii=False, indent=2)

print(f"\n✓ {len(data)} Einträge gespeichert → {output_path}")