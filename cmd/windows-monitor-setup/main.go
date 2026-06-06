package main

import (
	"archive/zip"
	"encoding/json"
	"errors"
	"fmt"
	"io"
	"net/http"
	"os"
	"os/exec"
	"path/filepath"
	"runtime"
	"strings"
	"syscall"
	"time"
	"unsafe"
)

const (
	appName       = "windows-monitor"
	displayName   = "灵讯哨"
	author        = "tegic"
	releaseAPIURL = "https://api.github.com/repos/teg1c/windows-monitor/releases/latest"
)

var version = "1.0.0"

type releaseInfo struct {
	Assets []struct {
		Name               string `json:"name"`
		BrowserDownloadURL string `json:"browser_download_url"`
	} `json:"assets"`
}

func main() {
	quiet := hasArg("--quiet")
	uninstall := hasArg("--uninstall")
	installDir := filepath.Join(os.Getenv("LOCALAPPDATA"), "Programs", appName)

	if uninstall {
		if err := uninstallApp(installDir); err != nil {
			fail(err, quiet)
		}
		info(displayName+" 已卸载。", quiet)
		return
	}

	if exists(installDir) && !quiet {
		choice := messageBox(displayName+" 已安装。\n\n是：安装/更新\n否：卸载\n取消：退出", displayName+" 安装程序", 0x00000003|0x00000020)
		if choice == 2 {
			return
		}
		if choice == 7 {
			if err := uninstallApp(installDir); err != nil {
				fail(err, quiet)
			}
			info(displayName+" 已卸载。", quiet)
			return
		}
	} else if !quiet {
		choice := messageBox(fmt.Sprintf("是否安装 %s？\n\n安装目录：%s", displayName, installDir), displayName+" 安装程序", 0x00000004|0x00000020)
		if choice != 6 {
			return
		}
	}

	if err := installApp(installDir); err != nil {
		fail(err, quiet)
	}
	info(displayName+" 已安装完成。", quiet)
	_ = exec.Command(filepath.Join(installDir, appName+".exe")).Start()
}

func installApp(installDir string) error {
	stopApp()
	if err := os.MkdirAll(installDir, 0755); err != nil {
		return err
	}
	if err := clearInstallDir(installDir); err != nil {
		return err
	}

	zipPath, cleanup, err := resolveZip()
	if err != nil {
		return err
	}
	if cleanup != nil {
		defer cleanup()
	}
	if err := unzip(zipPath, installDir); err != nil {
		return err
	}
	if err := copySelf(filepath.Join(installDir, appName+"-setup.exe")); err != nil {
		return err
	}
	if err := createShortcuts(installDir); err != nil {
		return err
	}
	return writeUninstallEntry(installDir)
}

func uninstallApp(installDir string) error {
	stopApp()
	_ = removeShortcuts()
	_ = deleteUninstallEntry()

	current, _ := os.Executable()
	if strings.HasPrefix(strings.ToLower(current), strings.ToLower(installDir)) {
		cmd := exec.Command("cmd.exe", "/C", fmt.Sprintf("ping 127.0.0.1 -n 2 > nul & rmdir /S /Q %q", installDir))
		cmd.SysProcAttr = &syscall.SysProcAttr{HideWindow: true}
		return cmd.Start()
	}
	return os.RemoveAll(installDir)
}

func resolveZip() (string, func(), error) {
	exe, _ := os.Executable()
	adjacent := filepath.Join(filepath.Dir(exe), appName+".zip")
	if exists(adjacent) {
		return adjacent, nil, nil
	}

	url, err := latestAssetURL(appName + ".zip")
	if err != nil {
		return "", nil, err
	}
	tmp := filepath.Join(os.TempDir(), fmt.Sprintf("%s-%d.zip", appName, time.Now().UnixNano()))
	if err := download(url, tmp); err != nil {
		return "", nil, err
	}
	return tmp, func() { _ = os.Remove(tmp) }, nil
}

func latestAssetURL(assetName string) (string, error) {
	req, err := http.NewRequest(http.MethodGet, releaseAPIURL, nil)
	if err != nil {
		return "", err
	}
	req.Header.Set("User-Agent", appName+"-setup/"+version)

	resp, err := http.DefaultClient.Do(req)
	if err != nil {
		return "", err
	}
	defer resp.Body.Close()
	if resp.StatusCode < 200 || resp.StatusCode >= 300 {
		return "", fmt.Errorf("GitHub 返回 HTTP %d", resp.StatusCode)
	}

	var release releaseInfo
	if err := json.NewDecoder(resp.Body).Decode(&release); err != nil {
		return "", err
	}
	for _, asset := range release.Assets {
		if strings.EqualFold(asset.Name, assetName) {
			return asset.BrowserDownloadURL, nil
		}
	}
	return "", errors.New("最新 Release 中没有找到 " + assetName)
}

func download(url, target string) error {
	req, err := http.NewRequest(http.MethodGet, url, nil)
	if err != nil {
		return err
	}
	req.Header.Set("User-Agent", appName+"-setup/"+version)
	resp, err := http.DefaultClient.Do(req)
	if err != nil {
		return err
	}
	defer resp.Body.Close()
	if resp.StatusCode < 200 || resp.StatusCode >= 300 {
		return fmt.Errorf("下载失败：HTTP %d", resp.StatusCode)
	}

	out, err := os.Create(target)
	if err != nil {
		return err
	}
	defer out.Close()
	_, err = io.Copy(out, resp.Body)
	return err
}

func unzip(zipPath, targetDir string) error {
	reader, err := zip.OpenReader(zipPath)
	if err != nil {
		return err
	}
	defer reader.Close()

	for _, file := range reader.File {
		target := filepath.Join(targetDir, file.Name)
		if !strings.HasPrefix(filepath.Clean(target), filepath.Clean(targetDir)+string(os.PathSeparator)) && filepath.Clean(target) != filepath.Clean(targetDir) {
			return errors.New("压缩包包含非法路径：" + file.Name)
		}
		if file.FileInfo().IsDir() {
			if err := os.MkdirAll(target, file.Mode()); err != nil {
				return err
			}
			continue
		}
		if err := os.MkdirAll(filepath.Dir(target), 0755); err != nil {
			return err
		}
		input, err := file.Open()
		if err != nil {
			return err
		}
		output, err := os.OpenFile(target, os.O_CREATE|os.O_TRUNC|os.O_WRONLY, file.Mode())
		if err != nil {
			_ = input.Close()
			return err
		}
		_, copyErr := io.Copy(output, input)
		closeErr := output.Close()
		_ = input.Close()
		if copyErr != nil {
			return copyErr
		}
		if closeErr != nil {
			return closeErr
		}
	}
	return nil
}

func clearInstallDir(installDir string) error {
	entries, err := os.ReadDir(installDir)
	if os.IsNotExist(err) {
		return nil
	}
	if err != nil {
		return err
	}
	for _, entry := range entries {
		if strings.EqualFold(entry.Name(), "config.local.json") || strings.EqualFold(entry.Name(), "logs") {
			continue
		}
		if err := os.RemoveAll(filepath.Join(installDir, entry.Name())); err != nil {
			return err
		}
	}
	return nil
}

func copySelf(target string) error {
	current, err := os.Executable()
	if err != nil {
		return err
	}
	if strings.EqualFold(current, target) {
		return nil
	}
	input, err := os.Open(current)
	if err != nil {
		return err
	}
	defer input.Close()
	output, err := os.Create(target)
	if err != nil {
		return err
	}
	defer output.Close()
	_, err = io.Copy(output, input)
	return err
}

func stopApp() {
	cmd := exec.Command("taskkill.exe", "/IM", appName+".exe", "/F")
	cmd.SysProcAttr = &syscall.SysProcAttr{HideWindow: true}
	_ = cmd.Run()
}

func createShortcuts(installDir string) error {
	desktop := filepath.Join(os.Getenv("USERPROFILE"), "Desktop", displayName+".lnk")
	startMenu := filepath.Join(os.Getenv("APPDATA"), "Microsoft", "Windows", "Start Menu", "Programs", displayName)
	if err := os.MkdirAll(startMenu, 0755); err != nil {
		return err
	}
	appExe := filepath.Join(installDir, appName+".exe")
	setupExe := filepath.Join(installDir, appName+"-setup.exe")
	if err := shortcut(desktop, appExe, "", displayName); err != nil {
		return err
	}
	if err := shortcut(filepath.Join(startMenu, displayName+".lnk"), appExe, "", displayName); err != nil {
		return err
	}
	return shortcut(filepath.Join(startMenu, "卸载 "+displayName+".lnk"), setupExe, "--uninstall", "卸载 "+displayName)
}

func removeShortcuts() error {
	_ = os.Remove(filepath.Join(os.Getenv("USERPROFILE"), "Desktop", displayName+".lnk"))
	return os.RemoveAll(filepath.Join(os.Getenv("APPDATA"), "Microsoft", "Windows", "Start Menu", "Programs", displayName))
}

func shortcut(path, target, args, description string) error {
	script := fmt.Sprintf(`$s=New-Object -ComObject WScript.Shell;$l=$s.CreateShortcut(%s);$l.TargetPath=%s;$l.Arguments=%s;$l.WorkingDirectory=%s;$l.IconLocation=%s;$l.Description=%s;$l.Save()`,
		psQuote(path), psQuote(target), psQuote(args), psQuote(filepath.Dir(target)), psQuote(target+",0"), psQuote(description))
	return powershell(script)
}

func writeUninstallEntry(installDir string) error {
	appExe := filepath.Join(installDir, appName+".exe")
	setupExe := filepath.Join(installDir, appName+"-setup.exe")
	key := `HKCU\Software\Microsoft\Windows\CurrentVersion\Uninstall\` + appName
	commands := [][]string{
		{"add", key, "/f"},
		{"add", key, "/v", "DisplayName", "/d", displayName, "/f"},
		{"add", key, "/v", "DisplayVersion", "/d", version, "/f"},
		{"add", key, "/v", "Publisher", "/d", author, "/f"},
		{"add", key, "/v", "InstallLocation", "/d", installDir, "/f"},
		{"add", key, "/v", "DisplayIcon", "/d", appExe, "/f"},
		{"add", key, "/v", "UninstallString", "/d", fmt.Sprintf(`"%s" --uninstall`, setupExe), "/f"},
		{"add", key, "/v", "QuietUninstallString", "/d", fmt.Sprintf(`"%s" --uninstall --quiet`, setupExe), "/f"},
		{"add", key, "/v", "NoModify", "/t", "REG_DWORD", "/d", "1", "/f"},
		{"add", key, "/v", "NoRepair", "/t", "REG_DWORD", "/d", "1", "/f"},
	}
	for _, args := range commands {
		cmd := exec.Command("reg.exe", args...)
		cmd.SysProcAttr = &syscall.SysProcAttr{HideWindow: true}
		if err := cmd.Run(); err != nil {
			return err
		}
	}
	return nil
}

func deleteUninstallEntry() error {
	cmd := exec.Command("reg.exe", "delete", `HKCU\Software\Microsoft\Windows\CurrentVersion\Uninstall\`+appName, "/f")
	cmd.SysProcAttr = &syscall.SysProcAttr{HideWindow: true}
	_ = cmd.Run()
	return nil
}

func powershell(script string) error {
	cmd := exec.Command("powershell.exe", "-NoProfile", "-ExecutionPolicy", "Bypass", "-Command", script)
	cmd.SysProcAttr = &syscall.SysProcAttr{HideWindow: true}
	return cmd.Run()
}

func psQuote(value string) string {
	return "'" + strings.ReplaceAll(value, "'", "''") + "'"
}

func hasArg(name string) bool {
	for _, arg := range os.Args[1:] {
		if strings.EqualFold(arg, name) {
			return true
		}
	}
	return false
}

func exists(path string) bool {
	_, err := os.Stat(path)
	return err == nil
}

func info(message string, quiet bool) {
	if !quiet {
		messageBox(message, displayName+" 安装程序", 0x00000000|0x00000040)
	}
}

func fail(err error, quiet bool) {
	if !quiet {
		messageBox("操作失败："+err.Error(), displayName+" 安装程序", 0x00000000|0x00000010)
	}
	os.Exit(1)
}

func messageBox(text, caption string, flags uintptr) int {
	if runtime.GOOS != "windows" {
		fmt.Println(caption + ": " + text)
		return 0
	}
	user32 := syscall.NewLazyDLL("user32.dll")
	messageBoxW := user32.NewProc("MessageBoxW")
	ret, _, _ := messageBoxW.Call(
		0,
		uintptr(unsafe.Pointer(syscall.StringToUTF16Ptr(text))),
		uintptr(unsafe.Pointer(syscall.StringToUTF16Ptr(caption))),
		flags,
	)
	return int(ret)
}
