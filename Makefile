LICENSE-3rdparty.csv: sticker-award/pom.xml user-management/Stickerlandia.UserManagement.sln
	cd sticker-award && mvn license:aggregate-download-licenses
	python3 tools/licenses/convert_licenses.py
	cd user-management && dotnet restore
	python3 tools/licenses/convert_dotnet_licenses.py

all: LICENSE-3rdparty.csv

clean: 
	rm -f LICENSE-3rdparty.csv sticker-award/target/generated-sources/license/THIRD-PARTY.txt sticker-award/target/generated-sources/license/licenses.xml