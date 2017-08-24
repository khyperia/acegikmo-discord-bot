ifneq ($(wildcard /usr/include/openssl-1.0),)
	export OPENSSL_INCLUDE_DIR=/usr/include/openssl-1.0
else ifneq ($(wildcard /usr/local/opt/openssl/include),)
	export OPENSSL_INCLUDE_DIR=/usr/local/opt/openssl/include
endif
ifneq ($(wildcard /usr/lib/openssl-1.0),)
	export OPENSSL_LIB_DIR=/usr/lib/openssl-1.0
else ifneq ($(wildcard /usr/local/opt/openssl/lib),)
	export OPENSSL_LIB_DIR=/usr/local/opt/openssl/lib
endif

.PHONY: all install install_test test run format clippy
all: src/*
	cargo build --release

all_debug: src/*
	cargo build

format:
	cargo fmt -- --write-mode=overwrite

clippy:
	#rustup run nightly cargo rustc --features clippy -- -Z no-trans -Z extra-plugins=clippy
	rustup run nightly cargo clippy
