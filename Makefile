.PHONY: LICENSE-3rdparty.csv

LICENSE-3rdparty.csv:
	dd-license-attribution https://github.com/DataDog/stickerlandia > LICENSE-3rdparty.csv


clean: 
	rm -f LICENSE-3rdparty.csv
