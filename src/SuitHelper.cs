namespace CompetitiveCompany;

public static class SuitHelper {
    public static UnlockableItem GetOriginalSuit(StartOfRound playerManager) {
        return playerManager.unlockablesList.unlockables.Find(item => item.suitMaterial != null && item.alreadyUnlocked);
    }

    public static UnlockableItem CreateSuit(string name, StartOfRound playerManager) {
        var original = GetOriginalSuit(playerManager);
        
        return new UnlockableItem {
            unlockableName = name,
            prefabObject = original.prefabObject,
            unlockableType = original.unlockableType,
            shopSelectionNode = original.shopSelectionNode,
            alwaysInStock = original.alwaysInStock,
            IsPlaceable = original.IsPlaceable,
            hasBeenMoved = original.hasBeenMoved,
            placedPosition = original.placedPosition,
            placedRotation = original.placedRotation,
            inStorage = original.inStorage,
            canBeStored = original.canBeStored,
            maxNumber = original.maxNumber,
            hasBeenUnlockedByPlayer = original.hasBeenUnlockedByPlayer,
            suitMaterial = original.suitMaterial,
            headCostumeObject = original.headCostumeObject,
            lowerTorsoCostumeObject = original.lowerTorsoCostumeObject,
            alreadyUnlocked = original.alreadyUnlocked,
            unlockedInChallengeFile = original.unlockedInChallengeFile,
            spawnPrefab = original.spawnPrefab,
            jumpAudio = original.jumpAudio
        };
    }
}