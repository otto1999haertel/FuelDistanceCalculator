# extract costs per kilometer from pdf and create json
# file must be in download folder of system => check for that
# enhance model key with power (e.g. 100kw)
import os
import camelot
from pathlib import Path

Input_PDF = "autokostenuebersicht.pdf"

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
    print(table.df)