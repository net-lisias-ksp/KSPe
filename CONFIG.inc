SCRIPT_DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" &> /dev/null && pwd )"
source ${SCRIPT_DIR}/../CONFIG.inc

PACKAGE=KSPe
TARGETDIR="000_$PACKAGE"
TARGETBINDIR="$TARGETDIR/Plugins"
PROJECTSDIR="Source/$PACKAGE/"
VERSIONFILE="KSPe.version"

DLLS=""
# PluginData DLLs
PD_DLLS=""
# Rule oriented deployoment for PluginData
declare -A PD_SUB_RULES=( )
PD_SUB_DLLS=`{ for K in "${!PD_SUB_RULES[@]}"; do echo $(basename ${PD_SUB_RULES[$K]}); done } | sort -u | tr '\n' ' '`
PD_SUB_DIRS=`{ for K in "${!PD_SUB_RULES[@]}"; do echo $(dirname ${PD_SUB_RULES[$K]}); done } | sort -u | tr '\n' ' '`

# GameData deployed DLLs
GD_DLLS=""
GD_PRIORITY="000"

# Dev LIB deployed DLLs
LIB_DLLS="KSPe.Light.DOE ${DLLS}"


# External Libs from the LIB directory
EXT_DLLS=""


PROJECT_BRANCH=`git branch | grep \* | cut -d ' ' -f2`
if [ "$PROJECT_BRANCH" == "mestre" ] ; then
	# Release mode
	PROJECT_STATE=""
else
	# Note: Add a leading dash when this value is present!
	PROJECT_STATE=""
fi

VERSION=$( cat $VERSIONFILE | tr '\n' ' ' | sed -n -E 's/^.*?"VERSION\":\{.*"MAJOR":([0-9]+?),.*?"MINOR":([0-9]+?),.*?"PATCH":([0-9]+?),.*?"BUILD":([0-9]+?).*?\}.*$/\1.\2.\3.\4/p' )
