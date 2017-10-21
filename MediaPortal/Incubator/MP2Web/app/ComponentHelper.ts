// Source: http://www.syntaxsuccess.com/viewarticle/lazy-loading-in-angular-2.0
declare var System:any;

export class ComponentHelper{
    static LoadComponentAsync(name, path){
        return System.import(path).then(c => c[name]);
    }
}