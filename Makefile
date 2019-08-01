BASEURL=https://github.com/wasmerio/wasmer/releases/download/0.6.0/

all: 
	@echo "Use make fetch-runtimes to get the native runtimes to package"
	@echo "Use make docs 	       to do a full doc update pass (XML+website)"
	@echo "Use make doc-update     to update the XML API docs"
	@echo "Use make yaml           to produce the API docs for the website"

fetch-runtimes: native/wasmer_runtime_c_api.dll native/libwasmer_runtime_c_api.dylib native/libwasmer_runtime_c_api.so

native:
	mkdir native

native/wasmer_runtime_c_api.dll: native
	curl -L -o $@ $(BASEURL)/$(notdir $@)

native/libwasmer_runtime_c_api.dylib: native
	curl -L -o $@ $(BASEURL)/$(notdir $@)

native/libwasmer_runtime_c_api.so: native
	curl -L -o $@ $(BASEURL)/$(notdir $@)

docs: doc-update yaml

doc-update:
	mdoc update -i ./WasmerSharp/bin/Debug/netstandard2.0/WasmerSharp.xml -o ecmadocs/en ./WasmerSharp/bin/Debug/netstandard2.0/WasmerSharp.dll

yaml:
	-rm ecmadocs/en/ns-.xml
	mono /cvs/ECMA2Yaml/ECMA2Yaml/ECMA2Yaml/bin/Debug/ECMA2Yaml.exe --source=`pwd`/ecmadocs/en --output=`pwd`/docfx/api
	(cd docfx; mono ~/Downloads/docfx/docfx.exe build)


