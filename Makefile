RUNTIME ?= osx-arm64
CONFIGURATION ?= Release

DOTNET ?= $(shell dotnet --list-sdks 2>/dev/null | grep -q '^11\.' && echo dotnet || echo $(HOME)/.dotnet/dotnet)
export DOTNET_ROOT ?= $(if $(filter dotnet,$(DOTNET)),,$(HOME)/.dotnet)

READ_VERSION = sed -n 's|.*<Version>\(.*\)</Version>.*|\1|p' Directory.Build.props | head -1
VERSION ?= $(shell $(READ_VERSION))
export VERSION
export DOTNET

.PHONY: help
help: #shows what make commands are available
	@grep '^\S\S*: .*#' $(MAKEFILE_LIST) | sed 's/:.*#/: #/' | column -s '#' -t | sort

.PHONY: build
build: #builds the whole solution
	$(DOTNET) build SimplifyMySpace.slnx

.PHONY: test
test: #runs the Core tests
	$(DOTNET) test tests/Core.Tests

.PHONY: run
run: #builds and runs the app from source
	$(DOTNET) run --project src/App

.PHONY: check-windows
check-windows: #compiles the Windows build from macOS, trimming included
	$(DOTNET) publish src/App -c Release -r win-x64 --self-contained true -o build/check-windows

.PHONY: publish
publish: #trimmed single-file build for one runtime, make publish RUNTIME=win-x64
	$(DOTNET) publish src/App -c $(CONFIGURATION) -r $(RUNTIME) --self-contained true -o build/publish-$(RUNTIME)
	rm -f build/publish-$(RUNTIME)/*.pdb
	case "$(RUNTIME)" in osx-*) sh scripts/macos/thin.sh build/publish-$(RUNTIME) $(RUNTIME) ;; esac
	du -sh build/publish-$(RUNTIME)

.PHONY: bundle
bundle: #builds the signed macOS .app in build
	sh scripts/macos/bundle.sh

.PHONY: run-app
run-app: bundle #builds the .app and opens it (macOS)
	open "build/Simplify my Space.app"

.PHONY: pack
pack: #packs the Velopack release for one runtime, make pack RUNTIME=win-x64
	case "$(RUNTIME)" in win-*) RUNTIME=$(RUNTIME) sh scripts/windows/pack.sh ;; *) RUNTIME=$(RUNTIME) sh scripts/macos/pack.sh ;; esac

.PHONY: pack-macos
pack-macos: #packs the Velopack releases for both macOS architectures
	RUNTIME=osx-arm64 sh scripts/macos/pack.sh
	RUNTIME=osx-x64 sh scripts/macos/pack.sh
	@ls build/releases

.PHONY: pack-windows
pack-windows: #packs the Velopack releases for both Windows architectures, MSI and signing only on Windows
	RUNTIME=win-x64 sh scripts/windows/pack.sh
	RUNTIME=win-arm64 sh scripts/windows/pack.sh
	@ls build/releases

.PHONY: pack-all
pack-all: pack-macos pack-windows #packs every installer this machine can build

.PHONY: tools
tools: #installs the Velopack CLI the packaging scripts need
	dotnet tool install -g vpk --version 1.2.0 || dotnet tool update -g vpk --version 1.2.0

.PHONY: certificate
certificate: #creates the self-signed development certificate that keeps macOS permissions between builds
	sh scripts/macos/create-dev-certificate.sh

.PHONY: clean
clean: #removes build output
	$(DOTNET) clean SimplifyMySpace.slnx
	rm -rf build

.PHONY: version
version: #prints the current version
	@echo $(VERSION)

.PHONY: bump-patch
bump-patch: #bumps the patch version, 0.0.1 to 0.0.2
	@sh scripts/bump.sh patch

.PHONY: bump-minor
bump-minor: #bumps the minor version, 0.1.3 to 0.2.0
	@sh scripts/bump.sh minor

.PHONY: bump-major
bump-major: #bumps the major version, 0.2.3 to 1.0.0
	@sh scripts/bump.sh major

.PHONY: set-version
set-version: #sets an exact version, make set-version VERSION=1.4.0
	@sh scripts/bump.sh $(VERSION)

.PHONY: tag
tag: #tags HEAD as v<version> with its commit message and pushes it, which publishes a release
	@git diff --quiet && git diff --cached --quiet || { echo "commit your changes first"; exit 1; }
	@! git rev-parse -q --verify "refs/tags/v$(VERSION)" >/dev/null || { echo "v$(VERSION) already exists, bump the version first"; exit 1; }
	@git log -1 --format=%B | git tag -a "v$(VERSION)" -F -
	git push origin HEAD "v$(VERSION)"
	@echo "pushed v$(VERSION), the release workflow builds and publishes it"

.PHONY: art
art: #renders every asset (app icons, logo, README banner, installer art) from the app's own drawing code
	$(DOTNET) run --project scripts/art -- .
	sips -s format bmp build/art/msi-banner.png --out assets/installer/msi-banner.bmp >/dev/null
	sips -s format bmp build/art/msi-logo.png --out assets/installer/msi-logo.bmp >/dev/null
	iconutil -c icns build/art/AppIcon.iconset -o assets/icon/AppIcon.icns
	rm -rf build/art

.PHONY: sdk
sdk: #installs the .NET 11 SDK into ~/.dotnet when the system has none
	curl -sSL https://dot.net/v1/dotnet-install.sh | bash -s -- --channel 11.0 --install-dir $(HOME)/.dotnet

.PHONY: log
log: #follows the app log
	tail -f "$$HOME/Library/Application Support/SimplifyMySpace/log.txt"
