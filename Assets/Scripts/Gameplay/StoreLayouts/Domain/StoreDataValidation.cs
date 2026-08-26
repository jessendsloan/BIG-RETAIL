using System;
using System.Collections.Generic;

namespace BigRetail.StoreLayouts
{
    public enum StoreDataValidationCode
    {
        MissingData = 0,
        UnsupportedSchemaVersion = 1,
        MissingIdentifier = 2,
        MapMismatch = 3,
        MapFingerprintMismatch = 4,
        UnknownLandRegion = 5,
        OutsideMap = 6,
        DuplicateRecord = 7,
        MissingFoundation = 8,
        MissingFloor = 9,
        UnknownDefinition = 10,
        UnsupportedValue = 11,
        DuplicateInstanceId = 12,
        OccupiedCellOverlap = 13,
        MissingReference = 14,
        InvalidQuantity = 15,
        InvalidValue = 16
    }


    public readonly struct StoreDataValidationIssue
    {
        public StoreDataValidationCode Code { get; }

        public string Path { get; }

        public string Message { get; }


        public StoreDataValidationIssue(
            StoreDataValidationCode code,
            string path,
            string message)
        {
            Code = code;
            Path = path ?? string.Empty;
            Message = message ?? string.Empty;
        }


        public override string ToString()
        {
            return string.IsNullOrEmpty(Path)
                ? $"{Code}: {Message}"
                : $"{Code} at {Path}: {Message}";
        }
    }


    public sealed class StoreDataValidationResult
    {
        private readonly List<StoreDataValidationIssue> issues =
            new List<StoreDataValidationIssue>();


        public bool IsValid =>
            issues.Count == 0;

        public int IssueCount =>
            issues.Count;

        public IReadOnlyList<StoreDataValidationIssue> Issues =>
            issues;


        internal void Add(
            StoreDataValidationCode code,
            string path,
            string message)
        {
            issues.Add(
                new StoreDataValidationIssue(
                    code,
                    path,
                    message));
        }

        public bool Contains(
            StoreDataValidationCode code)
        {
            for (int index = 0;
                 index < issues.Count;
                 index++)
            {
                if (issues[index].Code == code)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
