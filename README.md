# ShowHost

A private, client-server based system for hosting and playing interactive ShowHosts over an encrypted connection.

The goal of this project is to provide a platform for hosting various ShowHost formats, particulary games inspired by formats featured on the [PietSmiet Youtube channel](https://www.youtube.com/@pietsmiet).

The project is primarily intended for private use and small groups rather than large-scale public hosting.

## Project status

ShowHost is currently under active development.

- [x] Foundation
- [x] Lobby System
- [x] Moderator controls
- [ ] Implement the Format **"57"** ([link](https://www.youtube.com/watch?v=boFNtl8LjO8))
- [ ] Proper Server Multithreading and admin console
- [ ] Allow for private games / games that require a key

## Technology

- **Server:** .NET 10 Console Application
	- Cross-plattform / no OS-specific server dependency
	- Handles client connections, authentication and game states
	- Communication: TCP with with JSON data
	- Encryption: ECDH key exchange followed by symmetric encryption for communication
- **Client:** .NET 10 WPF Application
	- Windows only
	- Provides the player and moderator interfaces

## Development Philosohpy

This project is written and maintained by me.

**No AI-generated code is used in the project.** AI tools may only be used as a source of inspiration and as a helper on UI/UX design. All code is written by the author.

In particular, no code from AI responses is ever copied into the project. The intention si to keep the implementation understandable and fully under the author's control rather than relying on "vibe coding".

## Dependencies

### Serilog

The server uses [Serilog](https://serilog.net/) for structured logging.

Serilog is distributed under the **Apache License 2.0**.

## Font Acknowledgment

This project uses **Besley*** — an open-source serif typeface designed by Owen Earl (Indestructible Type*), inspired by the designs of Robert Besley. 
Besley* is distributed under the SIL Open Font License (OFL), which supports the free use and sharing of typefaces. This license can be found in the [Besley subdirectory](Client/Assets/Fonts/Besley/LICENSE.md).

For more information about the Besley Font, please visit their website, [indestructibletype.com/Besley.html](https://indestructibletype.com/Besley.html), and read the [README File](Client/Assets/Fonts/Besley/README.txt).

*I gratefully acknowledge and value the work of the designer and the open-source type community for making this font available.*

## Icon Acknowledgment

This project uses icons from **Feather Icons** — a collection of simple, beautiful, open-source icons.  
Feather Icons are distributed under the MIT License, which permits free use, modification, and distribution.

For more information about Feather Icons, please visit their website at [feathericons.com](https://feathericons.com/).

*I gratefully acknowledge the contributors and maintainers of Feather Icons for providing this clean and accessible icon set to the community.*

## Word List Acknowledgement

This project produces randomly generated game names using words from curated word lists.
These word lists can be found in the [words subdirecory](Server/Keys/Words).

The lists are borrowed from the [Adjective-colour-animal generator project](https://github.com/csmith/aca), which is published under the MIT License.
The ACA project uses word lists based heavily on the [unique-name-generator](https://github.com/andreasonny83/unique-names-generator) npm module, with various manual modifications and curation by its author.

*I gratefully acknowledge the authors and contributors of both projects for their work in creating and curating these word lists and for making them available to the open-source community.*

## License

The source code of **ShowHost** is licensed under the **MIT License**.

This license applies to the original code written for this project. Third-party software, libraries, fonts and other assets included with or used by the project remain subject to their respective licenses.

The full MIT License is available in the [LICENSE File](LICENSE)

### Third-Party Licenses

The project uses third-party software and assets that are distributed under different licenses:

| Component | License |
| --- | --- |
| Serilog | Apache License 2.0 |
| Besley | SIL Open Font License 1.1 |
| Feather Icons | MIT License |
| Word Lists | MIT License |

Their licenses and attribution information are documented in [THIRD_PARTY_NOTICES.md](THIRD_PARTY_NOTICES.md).

Third-party licenses are not replaced or superseded by the MIT License covering the original ShowHost source code.
