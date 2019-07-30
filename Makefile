all: doc-update yaml

doc-update:
	mdoc update -i ./WasmerSharp/bin/Debug/netstandard2.0/WasmerSharp.xml -o ecmadocs/en ./WasmerSharp/bin/Debug/netstandard2.0/WasmerSharp.dll

yaml:
	-rm ecmadocs/en/ns-.xml
	mono /cvs/ECMA2Yaml/ECMA2Yaml/ECMA2Yaml/bin/Debug/ECMA2Yaml.exe --source=`pwd`/ecmadocs/en --output=`pwd`/docfx/api
	(cd docfx; mono ~/Downloads/docfx/docfx.exe build)


