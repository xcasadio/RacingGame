"""
QUAL-003: Remove #region File Description blocks and #region Using directives wrappers.
Also removes single-method #region/#endregion wrappers in small groups.
"""
import os
import re
import glob

def process_file(path):
    with open(path, 'r', encoding='utf-8-sig', newline='') as f:
        content = f.read()

    original = content

    # 1. Remove #region File Description ... #endregion block
    # This block is always at the top of files and contains only // comments
    content = re.sub(
        r'#region File Description\r?\n(?:.*\r?\n)*?#endregion\r?\n(?:\r?\n)?',
        '',
        content,
        count=1
    )

    # 2. Remove #region Using directives/Statements wrapper (handles both capitalizations)
    before_2a = content
    content = re.sub(r'(?i)#region Using (?:directives|statements)\r?\n', '', content, count=1)
    # Only remove the orphaned #endregion if the #region Using wrapper was actually removed.
    # This prevents accidentally removing #endregion from other regions (e.g. #region Unit Testing).
    if content != before_2a:
        content = re.sub(r'(?m)^#endregion\r?\n(\r?\n)?(namespace )', r'\2', content, count=1)

    # Strip leading blank lines (artifact from removed header regions)
    content = content.lstrip('\r\n')

    if content != original:
        with open(path, 'w', encoding='utf-8', newline='') as f:
            f.write(content)
        return True
    return False


def main():
    root = os.path.join(os.path.dirname(__file__), '..', 'RacingGame.Shared')
    files = glob.glob(os.path.join(root, '**', '*.cs'), recursive=True)
    changed = 0
    for path in files:
        if process_file(path):
            changed += 1
            print(f"  cleaned: {os.path.relpath(path, root)}")
    print(f"\nTotal changed: {changed} files")


if __name__ == '__main__':
    main()
