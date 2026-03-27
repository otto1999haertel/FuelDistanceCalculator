# extract costs per kilometer from pdf and create json
# file must be in download folder of system => check for that
# enhance model key with power (e.g. 100kw)
import os
import camelot
from pathlib import Path

Input_PDF = "autokostenuebersicht.pdf"
Output_JSON = "ADAC_car_data.json"

current_dir = os.path.dirname(os.path.abspath(__file__))
output_json_dir = os.path.join(current_dir, "..", "FuelDistanceCalculator", "Data")
output_json_dir = os.path.abspath(output_json_dir)

downloads_path = str(Path.home() / "Downloads")
print(f"Looking for PDF in {downloads_path}")
file_path = f"{downloads_path}/{Input_PDF}"

if not os.path.exists(file_path):
    raise(f"File {Input_PDF} not found in {downloads_path}")

if not os.path.exists(output_json_dir):
    raise(f"Output dir {output_json_dir} not found")

camelot_tables = camelot.read_pdf(file_path, pages="3-end", flavor="stream")
for table in camelot_tables:
    #Row index 4 is brand
    #First Column is model
    #Second Column is power
    #Last Column is cost per kilometer
    current_brand= table.df.iloc[4,0]
    for row in table.data[5:]:
        model = row[0]
        power = row[1]
        cost_per_km = row[-1]
        print(f"Brand: {current_brand}, Model: {model}, Power: {power}, Cost per km: {cost_per_km}")
    print(table.df)