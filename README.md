[![License](https://img.shields.io/badge/License-MIT-blue.svg)](https://opensource.org/licenses/MIT) [![#yourfirstpr](https://img.shields.io/badge/first--timers--only-friendly-blue.svg)](https://github.com/nanoframework/Home/blob/master/CONTRIBUTING.md) [![Build Status](https://dev.azure.com/nanoframework/hex2dfu/_apis/build/status/nanoframework.hex2dfu?branchName=master)](https://dev.azure.com/nanoframework/hex2dfu/_build/latest?definitionId=41&branchName=master) [![Discord](https://img.shields.io/discord/478725473862549535.svg)](https://discord.gg/gCyBu8T)

![nanoFramework logo](https://github.com/nanoframework/Home/blob/master/resources/logo/nanoFramework-repo-logo.png)

-----

### Welcome to the **nanoFramework** HEX2DFU tool repository!

This repo contains the Hex2Dfu tool.
It's a console app to convert and/or pack HEX or BIN files in DFU packages (required to update some target boards).
Is part of **nanoFramework** toolbox, along with other various tools that are required in **nanoFramework** development, usage or repository management.

## Usage

### Create a DFU file from a single hex file

To create a DFU package from a single hex file use the following command line:

```cmd
hex2dfu -h=hex_file_name -o=output_DFU_image_file_name

```

### Create a DFU file from one or more bin files

To create a DFU package from one (or more) bin files use the following command line:

```cmd
hex2dfu -b=bin_file_name -a=address_to_flash [-b=bin_file_name_N -a=address_to_flash_N] -o=output_DFU_image_file_name

```

### Optional parameters

The following parameters are available and are optional.

#### Set the VID of target USB device

Allows setting the VID of the USB device. Hexadecimal format. If not specified the STM default will be used. Usually used in conjunction with PID.

```cmd
hex2dfu -h=hex_file_name -o=output_DFU_image_file_name [-v="0000"]

```

#### Set the PID of target USB device

Allows setting the PID of the USB device. Hexadecimal format. If not specified the STM default will be used. Usually used in conjunction with VID.

```cmd
hex2dfu -h=hex_file_name -o=output_DFU_image_file_name [-p="0000"]

```

#### Set the firmware version of the target device

Allows setting the firmware version of the target device. Hexadecimal format. If not specified the STM default will be used. Can be used by the DFU tool to check for a valid device to update.

```cmd
hex2dfu -h=hex_file_name -o=output_DFU_image_file_name [-f=""0000""]

```

## Feedback and documentation

For documentation, providing feedback, issues and finding out how to contribute please refer to the [Home repo](https://github.com/nanoframework/Home).

Join our Discord community [here](https://discord.gg/gCyBu8T).

## Credits

The list of contributors to this project can be found at [CONTRIBUTORS](https://github.com/nanoframework/Home/blob/master/CONTRIBUTORS.md).

## License

The **nanoFramework** HEX2DFU tool is licensed under the [MIT license](https://opensource.org/licenses/MIT).

## Code of Conduct
This project has adopted the code of conduct defined by the [Contributor Covenant](http://contributor-covenant.org/)
to clarify expected behavior in our community.
