import json
import os
import re
import camelot
from pathlib import Path

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

        # Brand-Zeile erkennen: power und cost_per_km sind leer
        if not power and not cost_per_km:
            if "EUR/h" in model:
                brand = re.sub(r'\s*\d+\s*EUR/h.*', '', model).strip()
            else:
                # Fragment gehört zum vorherigen Modellnamen
                # Letzten Key updaten
                if data:
                    last_key = list(data.keys())[-1]
                    last_value = data.pop(last_key)
                    # Fragment vor dem Power-Teil einfügen
                    parts = last_key.rsplit(' ', 1)  # trennt die Power-Zahl ab
                    new_key = f"{parts[0]} {model} {parts[1]}".strip()
                    data[new_key] = last_value
            continue

        if cost_per_km and re.match(r'^\d+[,\.]\d+$', cost_per_km.strip()):
            cost_per_km = str(round(float(cost_per_km.replace(",", ".")) / 100, 4))
            key = f"{brand} {model} {power}".strip()
            data[key] = cost_per_km.replace(",", ".")
            print(f"✓ {key}: {data[key]}")

output_path = os.path.join(output_json_dir, Output_JSON)
with open(output_path, "w", encoding="utf-8") as f:
    json.dump(data, f, ensure_ascii=False, indent=2)

print(f"\n✓ {len(data)} Einträge gespeichert → {output_path}")